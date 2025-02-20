import tiktoken
from openai import OpenAI

import datetime
import re
import shutil
from num2words import num2words

from character import *
from utils import *
from MemorySystem import MemorySystem

# Настройка логирования
import logging
import colorlog

# Настройка цветного логирования
handler = colorlog.StreamHandler()
handler.setFormatter(colorlog.ColoredFormatter(
    '%(log_color)s%(asctime)s - %(levelname)s - %(message)s',
    log_colors={
        'INFO': 'white',
        'WARNING': 'yellow',
        'ERROR': 'red',
        'CRITICAL': 'red,bg_white',
    }
))

logger = colorlog.getLogger(__name__)
logger.setLevel(logging.INFO)
logger.addHandler(handler)


class ChatModel:
    def __init__(self, gui, api_key, api_key_res, api_url, api_model, api_make_request):

        self.gui = gui

        try:
            self.api_key = api_key
            self.api_key_res = api_key_res
            self.api_url = api_url
            self.api_model = api_model
            self.makeRequest = api_make_request

            self.client = OpenAI(api_key=self.api_key, base_url=self.api_url)
            print("Со старта удалось запустить OpenAi client")
        except:
            print("Со старта не получилось запустить OpenAi client")

        try:
            self.tokenizer = tiktoken.encoding_for_model("gpt-4o-mini")
            self.hasTokenizer = True
        except:
            print("Тиктокен не сработал(")
            self.hasTokenizer = False

        self.max_input_tokens = 2048
        self.max_response_tokens = 3200
        self.cost_input_per_1000 = 0.0432
        self.cost_response_per_1000 = 0.1728
        self.history_file = "chat_history.json"
        self.chat_history = self.load_history().get('messages', [])
        self.memory_limit = 40  # Ограничение сообщения

        self.attitude = 60
        self.boredom = 10
        self.stress = 5

        self.distance = 0.0
        self.roomPlayer = -1
        self.roomMita = -1

        self.nearObjects = ""
        self.actualInfo = ""
        self.LongMemoryRememberCount = 0

        self.infos = []

        self.SecretExposedText = ""
        self.secretExposed = False
        self.secretExposedFirst = False
        # Загрузка данных из файлов
        self.PlayingFirst = False

        self.init_characters()

        self.load_prompts()

        self.MitaLongMemory = {"role": "system", "content": f" LongMemory<  >EndLongMemory "}
        self.MemorySystem = MemorySystem("mita_memories.json")

        self.MitaMainBehaviour = []
        self.MitaExamples = []
        self.systemMessages = []

        self.HideAiData = True

    def init_characters(self):
        """
        Инициализует возможных персонажей
        :return:
        """
        self.crazy_mita_character = CrazyMita("Mita", "/speaker mita")
        self.cappy_mita_character = CappyMita("Cappy", "/speaker cap")
        self.cart_space = SpaceCartridge("Space", "/speaker  wheatley")
        self.kind_mita_character = KindMita("Kind", "/speaker  shorthair")

        self.current_character = self.cappy_mita_character



    def load_prompts(self):
        self.common = load_text_from_file("Prompts/CrazyMitaPrompts/Main/common.txt")
        self.main = load_text_from_file("Prompts/CrazyMitaPrompts/Main/main.txt")
        self.player = load_text_from_file("Prompts/CrazyMitaPrompts/Main/player.txt")
        self.mainPlaying = load_text_from_file("Prompts/CrazyMitaPrompts/Main/mainPlaing.txt")
        self.mainCrazy = load_text_from_file("Prompts/CrazyMitaPrompts/Main/mainCrazy.txt")

        self.examplesLong = load_text_from_file("Prompts/CrazyMitaPrompts/Context/examplesLong.txt")
        self.examplesLongCrazy = load_text_from_file("Prompts/CrazyMitaPrompts/Context/examplesLongCrazy.txt")

        self.world = load_text_from_file("Prompts/CrazyMitaPrompts/NotUsedNow/world.txt")
        self.mita_history = load_text_from_file("Prompts/CrazyMitaPrompts/Context/mita_history.txt")

        self.variableEffects = load_text_from_file("Prompts/CrazyMitaPrompts/Structural/VariablesEffects.txt")
        self.response_structure = load_text_from_file("Prompts/CrazyMitaPrompts/Structural/response_structure.txt")

        self.SecretExposedText = load_text_from_file("Prompts/CrazyMitaPrompts/Events/SecretExposed.txt")

    def calculate_cost(self, user_input):
        # Загружаем историю
        history_data = self.load_history()

        # Получаем только сообщения
        messages = history_data.get('messages', [])

        # Добавляем новое сообщение от пользователя
        messages.append({"role": "user", "content": user_input})

        # Считаем токены
        token_count = self.count_tokens(messages)

        # Рассчитываем стоимость
        cost = (token_count / 1000) * self.cost_input_per_1000

        return token_count, cost

    def count_tokens(self, messages):
        return sum(len(self.tokenizer.encode(msg["content"])) for msg in messages if
                   isinstance(msg, dict) and "content" in msg)

    def adjust_attitude(self, amount):
        amount = clamp(amount, -5, 5)
        """Корректируем отношение."""
        self.attitude = clamp(self.attitude + amount, 0, 100)
        print(f"Отношение изменилось на {amount}, новое значение: {self.attitude}")

    def adjust_boredom(self, amount):
        amount = clamp(amount, -5, 5)
        """Корректируем уровень скуки."""
        self.boredom = clamp(self.boredom + amount, 0, 100)
        print(f"Скука изменилась на {amount}, новое значение: {self.boredom}")

    def adjust_stress(self, amount):
        amount = clamp(amount, -5, 5)
        """Корректируем уровень стресса."""
        self.stress = clamp(self.stress + amount, 0, 100)
        print(f"Стресс изменился на {amount}, новое значение: {self.stress}")

    def update_openai_client(self, reserve_key=False):
        print("Попытка обновить клиент")
        if reserve_key:
            key = self.api_key_res
        else:
            key = self.api_key

        try:
            if self.api_url != "":
                print("И ключ и ссылка")
                self.client = OpenAI(api_key=key,
                                     base_url=self.api_url)
            else:
                print("Только ключ")
                self.client = OpenAI(api_key=key)
        except:
            print("update_openai_client не сработал")

    def generate_response(self, user_input, system_input=""):
        # Загрузка истории из файла
        history_data = self.load_history()

        messages = history_data.get('messages', [])
        if len(self.infos) > 0:
            print("Попытался расширить messages")
            messages.extend(self.infos)
            self.infos.clear()

        current_info = history_data.get('currentInfo', {})

        print(
            f"mood: {self.attitude}, secretExposed: {self.secretExposed}, secretExposedFirst: {self.secretExposedFirst}")

        # Логика для первой фазы (вводная информация)
        if len(messages) == 0:
            self._initialize_conversation()

        # Логика для поведения при игре с игроком
        elif self.attitude < 50 and not (self.secretExposed or self.PlayingFirst):
            self._start_playing_with_player()

        # Логика для раскрытия секрета
        elif (self.attitude <= 10 or self.secretExposed) and not self.secretExposedFirst:
            self._reveal_secret(messages)

        # Добавление информации о времени и пользовательского ввода
        messages = self._process_user_input(messages)

        messages = self._add_input(user_input, system_input, messages)

        # Ограничение на количество сообщений
        messages = messages[-self.memory_limit:]

        # Обновление текущего настроения
        timed_system_message = self._generate_timed_system_message()

        # Обновление текущей информации
        current_info.update({
            'MitaMainBehaviour': self.MitaMainBehaviour,
            'MitaExamples': self.MitaExamples,
            'timed_system_message': timed_system_message,
            'MitaLongMemory': self.MitaLongMemory
        })
        current_info['MitaSystemMessages'] = self.systemMessages

        combined_messages = self._combine_messages_Test2(self.current_character, messages, timed_system_message)

        # Генерация ответа с использованием клиента
        try:

            response, success = self._generate_chat_response(combined_messages)

            if not success:
                print("Неудачная генерация")
                return response
            elif response == "":
                print("Пустая генерация")
                return response

            response_message = {
                "role": "assistant",
                "content": response
            }
            messages.append(response_message)

            # Процессинг ответа: изменяем показатели и сохраняем историю
            response = self.process_response(user_input, response, messages)

            current_info.update({
                'MitaLongMemory': self.MitaLongMemory
            })

            print("До фразы")
            self.gui.textToTalk = self.process_text_to_voice(response)
            self.gui.textSpeaker = self.current_character.silero_command
            print("self.gui.textToTalk: " + self.gui.textToTalk)
            print("self.gui.textSpeaker: " + self.gui.textSpeaker)

            self.save_history({
                'messages': messages,
                'currentInfo': current_info,
                # Сохраняем переменные в историю
                'attitude': self.attitude,
                'boredom': self.boredom,
                'stress': self.stress,
                'secretExposed': self.secretExposed,
                'secretExposedFirst': self.secretExposedFirst
            })

            self.gui.update_debug_info()
            return response
        except Exception as e:
            print(f"Ошибка на фазе генерации: {e}")
            return f"Ошибка на фазе генерации: {e}"

    def _initialize_conversation(self):
        """Инициализация начальной беседы"""
        self.systemMessages.insert(0, {"role": "system", "content": f"{self.player}\n"})
        self.MitaExamples = {"role": "system", "content": f"{self.examplesLong}\n"}
        self.MitaMainBehaviour = {"role": "system", "content": f"{self.main}\n"}
        self.systemMessages.insert(0, {"role": "system", "content": f"{self.response_structure}"})

    def _start_playing_with_player(self):
        """Игровая логика, когда персонаж начинает играть с игроком"""
        print("Играет с игроком в якобы невиновную")
        self.PlayingFirst = True
        self.MitaMainBehaviour = {"role": "system", "content": f"{self.mainPlaying}\n"}
        self.current_character.replace_prompt("main", "mainPlaying")

    def _reveal_secret(self, messages):
        """Логика раскрытия секрета"""
        print("Перестала играть вообще")
        self.secretExposedFirst = True
        self.secretExposed = True
        self.MitaMainBehaviour = {
            "role": "system",
            "content": f"{self.mainCrazy}\n{self.response_structure}"
        }
        self.MitaExamples = {"role": "system", "content": f"{self.examplesLongCrazy}\n"}
        add_temporary_system_message(messages, f"{self.SecretExposedText}")
        system_message = {"role": "system", "content": f"{self.mita_history}\n"}
        self.systemMessages.append(system_message)

        self.current_character.replace_prompt("main", "mainCrazy")
        self.current_character.replace_prompt("mainPlaying", "mainCrazy")

    def _generate_timed_system_message(self, characher: Character = None):
        """Генерация сообщения с текущим состоянием персонажа"""

        if characher != self.crazy_mita_character:
            return None

        return {
            "role": "system",
            "content": (f"Твои характеристики. {self.variableEffects}"
                        f"Конкретно сейчас они следующие: "
                        f"Отношение: {self.attitude}/100."
                        f"Стресс: {self.stress}/100."
                        f"Скука: {self.boredom}/100."
                        f"Состояние секрета: {self.secretExposed}"
                        f"{self.common}")
        }

    def _process_user_input(self, messages):
        """
        Перед сообщением пользователя будет контекст, он не запишется в историю.
        :param messages:
        :return: сообщение с добавленным временным сообщением до
        """

        self.LongMemoryRememberCount += 1

        """Обработка пользовательского ввода и добавление сообщений"""
        date_now = datetime.datetime.now().replace(microsecond=0)

        repeated_system_message = f"Time: {date_now}. до игрока {self.distance}м. "

        if self.distance == 0:
            repeated_system_message = f"Time: {date_now}. до игрока ? м. "

        # Проверяем правильность вызова get_room_name
        repeated_system_message += f"You are in {self.get_room_name(int(self.roomMita))}, player is in {self.get_room_name(int(self.roomPlayer))}. "

        if self.LongMemoryRememberCount % 3 == 0:
            repeated_system_message += " Remember facts for 3 messages by using <+memory>high|The player attaked me</memory>"

        messages.append({"role": "system", "content": repeated_system_message})

        return messages

    def _add_input(self, user_input, system_input, messages):
        if system_input != "":
            messages.append({"role": "system", "content": system_input})
        if user_input != "":
            messages.append({"role": "user", "content": user_input})
        return messages

    def get_room_name(self, room_id):
        # Сопоставление ID комнаты с её названием
        room_names = {
            0: "Кухня",  # Кухня
            1: "Зал",  # Главная комната
            2: "Комната",  # Спальня
            3: "Туалет",  # Туалет
            4: "Подвал"  # Подвал
        }

        # Возвращаем название комнаты, если оно есть, иначе возвращаем сообщение о неизвестной комнате
        return room_names.get(room_id, "?")

    def _combine_messages_Test2(self, character: Character, messages, timed_system_message):
        """Комбинирование всех сообщений перед отправкой"""
        # Чем выше здесь, тем дальше от начала будет

        combined_messages = character.prepare_fixed_messages()

        # Добавляем timed_system_message, если оно не пусто и это словарь
        if timed_system_message and isinstance(timed_system_message, dict):
            combined_messages.append(timed_system_message)
            print("timed_system_message успешно добавлено.")

        if self.nearObjects != "" and self.nearObjects != "-":
            text = f"В радиусе от тебя следующие объекты (object tree) {self.nearObjects}"
            messageNear = {"role": "system", "content": text}
            combined_messages.append(messageNear)

        if self.actualInfo != "" and self.actualInfo != "-":
            messageActual = {"role": "system", "content": self.actualInfo}
            combined_messages.append(messageActual)

        # Добавляем messages, если они не пустые
        if messages:
            combined_messages.extend(messages)
            print("messages успешно добавлены. Количество:", len(messages))

        return combined_messages

    def _combine_messages(self, messages, timed_system_message):
        """Комбинирование всех сообщений перед отправкой"""
        combined_messages = []
        # Чем выше здесь, тем дальше от начала будет

        # Добавляем systemMessages, если они не пустые Здесь Как она отвечает и коммон скрипТ!
        if self.systemMessages:
            combined_messages.extend(self.systemMessages)
            print("systemMessages успешно добавлены. Количество:", len(self.systemMessages))

        # Добавляем MitaMainBehaviour, если это словарь
        if isinstance(self.MitaMainBehaviour, dict):
            combined_messages.append(self.MitaMainBehaviour)
            print("MitaMainBehaviour успешно добавлен.")

        # Добавляем MitaExamples, если это словарь
        if isinstance(self.MitaExamples, dict):
            combined_messages.append(self.MitaExamples)
            print("MitaExamples успешно добавлен.")

        # Добавляем MitaLongMemory, если это словарь и ключ "Role" существует и его значение не пустое
        if isinstance(self.MitaLongMemory, dict):
            if self.MitaLongMemory == {}:
                self.MitaLongMemory = {"role": "system", "content": f" LongMemory<  >EndLongMemory "}
            combined_messages.append(self.MitaLongMemory)
            print(self.MitaLongMemory)
            print("MitaLongMemory успешно добавлен.")
        else:
            print("MitaLongMemory не добавлен. Условие не выполнено.")

        # Добавляем timed_system_message, если это словарь
        if isinstance(timed_system_message, dict):
            combined_messages.append(timed_system_message)
            print("timed_system_message успешно добавлено.")

        if self.nearObjects != "" and self.nearObjects != "-":
            text = f"В радиусе от тебя следующие объекты (object tree) {self.nearObjects}"
            messageNear = {"role": "system", "content": text}
            combined_messages.append(messageNear)

        if self.actualInfo != "" and self.actualInfo != "-":
            messageActual = {"role": "system", "content": self.actualInfo}
            combined_messages.append(messageActual)

        # Добавляем messages, если они не пустые
        if messages:
            combined_messages.extend(messages)
            print("messages успешно добавлены. Количество:", len(messages))

        return combined_messages

    def _generate_chat_response(self, combined_messages, times=1):
        if times > 2:
            success = False
            return ""

        success = True
        response = None

        self._log_generation_start()
        self._save_and_calculate_cost(combined_messages)

        if self.makeRequest:
            formatted_messages = self._format_messages_for_gemini(combined_messages)
            response = self._generate_gemini_response(formatted_messages)
        else:
            response = self._generate_openai_response(combined_messages)

        if response:
            response = self._clean_response(response)
            logger.info("Мита: \n" + response)
        else:
            print("Ответ пустой в первый раз")

            self.update_openai_client(True)
            response = self._generate_chat_response(combined_messages, times + 1)
            if response:
                response = self._clean_response(response)
                logger.info("Мита: \n" + response)
            else:
                print("Ответ все еще пустой")
                success = False

        return response, success

    def _log_generation_start(self):
        logger.info("Перед отправкой на генерацию")
        logger.info(f"API Key: {SH(self.api_key)}")
        logger.info(f"API Key res: {SH(self.api_key_res)}")
        logger.info(f"API URL: {self.api_url}")
        logger.info(f"API Model: {self.api_model}")
        logger.info(f"Make Request: {self.makeRequest}")

    def _save_and_calculate_cost(self, combined_messages):
        save_combined_messages(combined_messages)
        try:
            self.gui.last_price = calculate_cost_for_combined_messages(self, combined_messages,
                                                                       self.cost_input_per_1000)
            logger.info(f"Calculated cost: {self.gui.last_price}")
        except Exception as e:
            logger.info("Не получилось сделать с токенайзером", str(e))

    def _format_messages_for_gemini(self, combined_messages):
        formatted_messages = []
        for msg in combined_messages:
            if msg["role"] == "system":
                formatted_messages.append({"role": "user", "content": f"[System Prompt]\n{msg['content']}"})
            else:
                formatted_messages.append(msg)
        save_combined_messages(formatted_messages, "Gem")
        return formatted_messages

    def _generate_gemini_response(self, formatted_messages):
        try:
            response = self.generate_responseGemini(formatted_messages)
            logger.info("Ответ Gemini: ", response)
            return response
        except Exception as e:
            logger.error("Что-то не так при генерации Gemini", str(e))
            return None

    def _generate_openai_response(self, combined_messages):
        if not self.client:
            logger.info("Попытка переподключения клиента")
            self.update_openai_client()

        try:

            # Гемини нужно всегда последнее сообщение пользователя
            if "gemini" in self.api_model and combined_messages[-1]["role"] == "system":
                print("gemini последнее системное сообщение")
                combined_messages[-1]["role"] = "user"
                combined_messages[-1]["content"] = "[SYSTEM INFO]" + combined_messages[-1]["content"]

            #print("in completion ", )
            completion = self.client.chat.completions.create(
                model=self.api_model,
                messages=combined_messages,
                max_tokens=self.max_response_tokens,
                presence_penalty=1.5,
                temperature=0.5,
            )
            response = completion.choices[0].message.content

            #print("out completion ", completion)
            return response.lstrip("\n")
        except Exception as e:
            logger.error("Что-то не так при генерации OpenAI", str(e))
            return None

    def _clean_response(self, response):
        try:
            response = response.lstrip("```\n")
            response = response.removesuffix("\n```\n")
        except Exception as e:
            logger.error("Проблема с префиксами или постфиксами", str(e))
        return response

    def generate_responseGemini(self, combined_messages):
        data = {
            "contents": [
                {"role": "user", "parts": [{"text": msg["content"]}]} for msg in combined_messages
            ],
            "generationConfig": {
                "maxOutputTokens": self.max_response_tokens,
                "temperature": 0.7,
                "presencePenalty": 1.5
            }
        }

        headers = {
            "Content-Type": "application/json",
            "Authorization": f"Bearer {self.api_key}"
        }

        logger.info("Отправляю запрос к Gemini")
        save_combined_messages(data, "Gem2")
        response = requests.post(self.api_url, headers=headers, json=data)

        if response.status_code == 200:
            response_data = response.json()
            generated_text = response_data.get("candidates", [{}])[0].get("content", {}).get("parts", [{}])[0].get(
                "text", "")
            logger.info("Gemini Flash: \n" + generated_text)
            return generated_text
        else:
            logger.error(f"Ошибка: {response.status_code}, {response.text}")
            return None

    def process_response(self, user_input, response, messages):

        try:

            # Обрабатываем изменения в памяти
            response = self.extract_and_process_memory_data(response)

            # Обрабатываем изменение состояния Секрета
            response = self.detect_secret_exposure(response)

            # Обрабатывает ответ, изменяет показатели на основе скрытой строки формата <p>x,x,x,x<p>.
            response = self.process_behavior_changes(response)

            #Выполняет команды - вынесено в логику юнити мода
            #response = self.process_commands(response, messages)

            # Возвращаем обработанный ответ для дальнейшей работы
            return response.strip()

        except Exception as e:
            print(f"Ошибка в обработке ответа: {e}")
            return response  # Возвращаем оригинальный ответ в случае ошибки

    def process_commands(self, response, messages):
        """
        Обрабатывает команды типа <c>...</c> в ответе.
        Команды могут быть: "Достать бензопилу", "Выключить игрока" и другие.
        """
        start_tag = "<c>"
        end_tag = "</c>"
        search_start = 0  # Указатель для поиска новых команд

        while start_tag in response[search_start:] and end_tag in response[search_start:]:
            try:
                # Находим команду
                start_index = response.index(start_tag, search_start) + len(start_tag)
                end_index = response.index(end_tag, start_index)
                command = response[start_index:end_index]

                # Логируем текущую команду
                print(f"Обработка команды: {command}")

                # Обработка команды
                if command == "Достать бензопилу":
                    ...
                    #add_temporary_system_message(messages, "Игрок был не распилен, произошла ошибка")

                    #if self.gui:
                    #   self.gui.close_app()

                elif command == "Выключить игрока":
                    ...
                    #add_temporary_system_message(messages, "Игрок был отпавлен в главное меню, но скоро он вернется...")

                    #if self.gui:
                    #   self.gui.close_app()

                else:
                    # Обработка неизвестных команд
                    #add_temporary_system_message(messages, f"Неизвестная команда: {command}")
                    print(f"Неизвестная команда: {command}")

                # Сдвигаем указатель поиска на следующий символ после текущей команды
                search_start = end_index + len(end_tag)

            except ValueError as e:
                add_temporary_system_message(messages, f"Ошибка обработки команды: {e}")
                break

        return response

    def process_text_to_voice(self, text):
        # Проверяем, что текст является строкой (если это байты, декодируем)
        if isinstance(text, bytes):
            try:
                text = text.decode("utf-8")  # Декодируем в UTF-8
            except UnicodeDecodeError:
                # Если UTF-8 не подходит, пробуем определить кодировку
                import chardet
                encoding = chardet.detect(text)["encoding"]
                text = text.decode(encoding)

        # Удаляем все теги и их содержимое
        clean_text = re.sub(r"<[^>]+>.*?</[^>]+>", "", text)
        clean_text = replace_numbers_with_words(clean_text)

        #clean_text = transliterate_english_to_russian(clean_text)

        # Если текст пустой, заменяем его на "Вот"
        if clean_text.strip() == "":
            clean_text = "Вот"

        return clean_text

    def extract_and_process_memory_data(self, response):
        """
        Извлекает данные из ответа с тегами памяти и выполняет операции.
        Форматы тегов:
        - Добавление: <+memory>priority|content</memory>
        - Обновление: <#memory>number|priority|content</memory>
        - Удаление: <-memory>number</memory>
        """
        # Регулярное выражение для захвата тегов памяти
        memory_pattern = r"<([+#-])memory>(.*?)</memory>"
        matches = re.findall(memory_pattern, response, re.DOTALL)

        if matches:
            print("Обнаружены команды изменения памяти!")
            for operation, content in matches:
                content = content.strip()
                try:
                    # Обработка добавления
                    if operation == "+":
                        parts = [p.strip() for p in content.split('|', 1)]
                        if len(parts) != 2:
                            raise ValueError("Неверный формат данных для добавления")

                        priority, mem_content = parts
                        self.MemorySystem.add_memory(
                            priority=priority,
                            content=mem_content
                        )
                        print(f"Добавлено воспоминание #{mem_content}")

                    # Обработка обновления
                    elif operation == "#":
                        parts = [p.strip() for p in content.split('|', 2)]
                        if len(parts) != 3:
                            raise ValueError("Неверный формат данных для обновления")

                        number, priority, mem_content = parts
                        self.MemorySystem.update_memory(
                            number=int(number),
                            priority=priority,
                            content=mem_content
                        )
                        print(f"Обновлено воспоминание #{number}")

                    # Обработка удаления
                    elif operation == "-":
                        number = content.strip()
                        self.MemorySystem.delete_memory(number=int(number))
                        print(f"Удалено воспоминание #{number}")

                    self.MitaLongMemory = {"role": "system", "content": self.MemorySystem.get_memories_formatted()}
                except Exception as e:
                    print(f"Ошибка обработки памяти: {str(e)}")

            # Удаление тегов из ответа
            if self.HideAiData:
                response = re.sub(memory_pattern, "", response, flags=re.DOTALL)

        return response

    def process_behavior_changes(self, response):
        """
        Обрабатывает изменения переменных на основе строки формата <p>x,x,x<p>.
        """
        start_tag = "<p>"
        end_tag = "</p>"

        if start_tag in response and end_tag in response:
            # Извлекаем изменения переменных
            start_index = response.index(start_tag) + len(start_tag)
            end_index = response.index(end_tag, start_index)
            changes_str = response[start_index:end_index]

            # Разделяем строку на отдельные значения
            changes = [float(x.strip()) for x in changes_str.split(",")]

            if len(changes) == 3:
                # Применяем изменения к переменным
                self.adjust_attitude(changes[0])
                self.adjust_boredom(changes[1])
                self.adjust_stress(changes[2])

            # Убираем строку с <p>...<p> из ответа
            if self.HideAiData:
                response = response[:response.index(start_tag)] + response[end_index + len(end_tag):]

        return response

    def detect_secret_exposure(self, response):
        """
        Проверяем, содержит ли ответ маркер <Secret!>, и удаляем его.
        """
        if "<Secret!>" in response:

            if not self.secretExposedFirst:
                self.secretExposed = True
                print(f"Секрет раскрыт")
                self.attitude = 15
                self.boredom = 20

            response = response.replace("<Secret!>", "")

        return response

    def reload_promts(self):
        print("Перезагрузка промптов")

        self.common = load_text_from_file("Prompts/CrazyMitaPrompts/Main/common.txt")
        self.main = load_text_from_file("Prompts/CrazyMitaPrompts/Main/main.txt")

        self.player = load_text_from_file("Prompts/CrazyMitaPrompts/Main/player.txt")
        self.mainPlaying = load_text_from_file("Prompts/CrazyMitaPrompts/Main/mainPlaing.txt")
        self.mainCrazy = load_text_from_file("Prompts/CrazyMitaPrompts/Main/mainCrazy.txt")

        self.examplesLong = load_text_from_file("Prompts/CrazyMitaPrompts/Context/examplesLong.txt")
        self.examplesLongCrazy = load_text_from_file("Prompts/CrazyMitaPrompts/Context/examplesLongCrazy.txt")

        self.world = load_text_from_file("Prompts/CrazyMitaPrompts/NotUsedNow/world.txt")
        self.mita_history = load_text_from_file("Prompts/CrazyMitaPrompts/Context/mita_history.txt")
        self.variableEffects = load_text_from_file("Prompts/CrazyMitaPrompts/Structural/VariablesEffects.txt")
        self.response_structure = load_text_from_file("Prompts/CrazyMitaPrompts/Structural/response_structure.txt")
        self.SecretExposedText = load_text_from_file("Prompts/CrazyMitaPrompts/Events/SecretExposed.txt")

        self.systemMessages.clear()

        self.systemMessages.append({"role": "system", "content": f"{self.response_structure}\n"})
        self.systemMessages.append({"role": "system", "content": f"{self.common}\n"})
        self.systemMessages.append({"role": "system", "content": f"{self.player}\n"})

        if self.secretExposed:
            self.MitaMainBehaviour = {"role": "system", "content": f"{self.mainCrazy}"}
            self.MitaExamples = {"role": "system", "content": f"{self.examplesLongCrazy}\n"}

            system_message = {"role": "system", "content": f"{self.mita_history}\n"}
            self.systemMessages.append(system_message)
        elif self.attitude < 50:
            self.MitaMainBehaviour = {"role": "system", "content": f"{self.mainPlaying}"}
        else:
            self.MitaMainBehaviour = {"role": "system", "content": f"{self.main}"}

    def load_history(self):
        """Загружаем историю из файла, создаем пустую структуру, если файл пуст или не существует."""
        try:
            with open(self.history_file, 'r', encoding='utf-8') as f:
                data = json.load(f)
                print("Загрузка истории")
                # Проверяем наличие ключей и их типов
                if (isinstance(data.get('messages'), list) and
                        isinstance(data.get('currentInfo'), dict) and
                        isinstance(data.get('MitaSystemMessages'), list)):

                    print("Историю получится заполнить")
                    # Загружаем переменные, если они есть в истории
                    self.attitude = data.get('attitude', 60)
                    self.boredom = data.get('boredom', 0)
                    self.stress = data.get('stress', 0)
                    self.secretExposed = data.get('secretExposed', False)
                    self.secretExposedFirst = data.get('secretExposedFirst', False)

                    currentInfo = data.get('currentInfo')
                    self.MitaMainBehaviour = currentInfo.get('MitaMainBehaviour', [])
                    self.MitaExamples = currentInfo.get('MitaExamples', [])
                    self.systemMessages = currentInfo.get('MitaSystemMessages', [])
                    self.MitaLongMemory = currentInfo.get('MitaLongMemory', {})

                    return data
                else:
                    print("Ошибка загрузки истории")
                    return self._default_history()
        except (json.JSONDecodeError, FileNotFoundError):
            # Если файл пуст или не существует, возвращаем структуру по умолчанию
            print("Ошибка загрузки истории")
            return self._default_history()

    def save_history(self, data):
        """Сохраняем историю в файл с явной кодировкой utf-8."""
        # Убедимся, что структура данных включает 'messages', 'currentInfo' и 'MitaSystemMessages'
        history_data = {
            'messages': data.get('messages', []),
            'currentInfo': data.get('currentInfo', {}),
            'MitaSystemMessages': data.get('MitaSystemMessages', []),
            # Сохраняем переменные в историю
            'attitude': self.attitude,
            'boredom': self.boredom,
            'stress': self.stress,
            'secretExposed': self.secretExposed,
            'secretExposedFirst': self.secretExposedFirst
        }

        with open(self.history_file, 'w', encoding='utf-8') as f:
            json.dump(history_data, f, ensure_ascii=False, indent=4)

    def save_chat_history(self):
        print("save_chat_history")
        # Имя исходного файла
        source_file = "chat_history.json"
        # Папка для сохранения историй
        target_folder = "SavedHistories"
        # Проверяем, существует ли папка SavedHistories, и создаём её, если нет
        os.makedirs(target_folder, exist_ok=True)

        # Формируем имя файла с таймингом
        timestamp = datetime.datetime.now().strftime("%d.%m.%Y_%H.%M")
        target_file = f"chat_history_{timestamp}.json"

        # Полный путь к новому файлу
        target_path = os.path.join(target_folder, target_file)

        # Копируем файл
        shutil.copy(source_file, target_path)
        print(f"Файл сохранён как {target_path}")

    def clear_history(self):
        print("ОЧИСТКА ИСТОРИИ!!!")
        """Очищаем историю чатов и восстанавливаем начальные значения переменных."""
        # Сбрасываем переменные к их значениям по умолчанию
        self.attitude = 60
        self.boredom = 0
        self.stress = 0
        self.secretExposed = False
        self.secretExposedFirst = False
        # Сохраняем пустую историю
        self.save_history(self._default_history())
        self.MitaLongMemory = {"role": "system", "content": f" LongMemory<  >EndLongMemory "}
        self.MemorySystem.clear_memories()

    def _default_history(self):
        print("ИСТОРИЧЕСКИЙ ДЕФОЛТ!!!")
        """Создаем структуру истории по умолчанию."""
        return {
            'messages': [],
            'currentInfo': {},
            'MitaSystemMessages': [],
            'attitude': 60,
            'boredom': 0,
            'stress': 0,
            'secretExposed': False,
            'secretExposedFirst': False,
        }


def add_temporary_system_message(messages, content):
    """
    Добавляет одноразовое системное сообщение в список сообщений.

    :param messages: Список сообщений, в который добавляется системное сообщение.
    :param content: Текст системного сообщения.
    """
    system_message = {
        "role": "system",
        "content": content
    }
    messages.append(system_message)


# Функция 1: Замена чисел на слова в русском тексте
def replace_numbers_with_words(text):
    numbers = re.findall(r'\d+', text)
    for number in numbers:
        word = num2words(int(number), lang='ru')
        text = text.replace(number, word)
    return text
