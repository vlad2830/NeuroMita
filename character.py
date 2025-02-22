from typing import Dict, List

from EventState import EventState
from MemorySystem import MemorySystem
from promptPart import PromptPart, PromptType
from HistoryManager import HistoryManager
from utils import load_text_from_file, clamp
import datetime
import re


class Character:
    def __init__(self, name: str, silero_command: str):

        self.name = name
        self.silero_command = silero_command

        self.fixed_prompts: List[PromptPart] = []
        self.float_prompts: List[PromptPart] = []
        self.temp_prompts: List[PromptPart] = []
        self.events: List[PromptPart] = []
        self.variables = []

        self.history_manager = HistoryManager(self.name)
        self.load_history()

        self.memory_system = MemorySystem(self.name)
        self.state = EventState()

        """
        Спорные временные переменные
        """
        self.LongMemoryRememberCount = 0
        self.MitaLongMemory = ""


        self.init()

    def add_prompt_part(self, part: PromptPart):
        if part.is_fixed:
            self.fixed_prompts.append(part)
        elif part.is_floating:
            self.float_prompts.append(part)
        elif part.is_temporary:
            self.temp_prompts.append(part)
        elif part.is_event:
            self.events.append(part)
        else:
            print("Добавляется неизвестный промпарт")

    def replace_prompt(self, name_current: str, name_next: str):
        """
        Заменяет активный промпт.

        :param name_current: Имя текущего активного промпта.
        :param name_next: Имя следующего промпта, который нужно активировать.
        """
        print("Замена промпарта")

        # Находим текущий активный промпт
        current_prompt = next((p for p in self.fixed_prompts if p.name == name_current), None)
        if current_prompt:
            current_prompt.active = False
        else:
            print(f"Промпт '{name_current}' не существует")

        # Находим следующий промпт
        next_prompt = next((p for p in self.fixed_prompts if p.name == name_next), None)
        if next_prompt:
            next_prompt.active = True
        else:
            print(f"Промпт '{name_next}' не существует")

    def prepare_fixed_messages(self) -> List[Dict]:
        """Создает фиксированные начальные установки
        :return: сообщения до
        """

        messages = []

        for part in self.fixed_prompts:
            if part.active:
                m = {"role": "system", "content": str(part)}
                messages.append(m)

        return messages

    def add_context(self, messages):
        """
        Перед сообщением пользователя будет контекст, он не запишется в историю.
        :param messages:
        :return: Messages с добавленным контекстом
        """

        self.LongMemoryRememberCount += 1

        """Обработка пользовательского ввода и добавление сообщений"""
        date_now = datetime.datetime.now().replace(microsecond=0)

        repeated_system_message = f"Time: {date_now}."

        if self.LongMemoryRememberCount % 3 == 0:
            repeated_system_message += " Remember facts for 3 messages by using <+memory>high|The player attaсked me</memory> (this text is example)"

        messages.append({"role": "system", "content": repeated_system_message})

        """
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
        """

        return messages

    def init(self):
        raise NotImplementedError("Метод init должен быть реализован в подклассе")

    def process_logic(self, messages: dict):
        """То, как должно что-то менсять до получения ответа"""
        print("Персонаж без изменяемой логики пропмтов")

    def process_response(self, response: str):
        response = self.extract_and_process_memory_data(response)
        """То, как должно что-то меняться в результате ответа"""
        return response
        print("Персонаж без изменяемой логики пропмтов")

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
                        self.memory_system.add_memory(
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
                        self.memory_system.update_memory(
                            number=int(number),
                            priority=priority,
                            content=mem_content
                        )
                        print(f"Обновлено воспоминание #{number}")

                    # Обработка удаления
                    elif operation == "-":
                        number = content.strip()
                        self.memory_system.delete_memory(number=int(number))
                        print(f"Удалено воспоминание #{number}")

                    self.MitaLongMemory = {"role": "system", "content": self.memory_system.get_memories_formatted()}
                except Exception as e:
                    print(f"Ошибка обработки памяти: {str(e)}")

        return response

    def load_history(self):
        """Кастомная обработка загрузки истории"""
        return self.history_manager.load_history()

    def safe_history(self, messages: dict, temp_context: dict):
        """Кастомная обработка сохранения истории"""

        history_data = {
            'fixed_parts': self.prepare_fixed_messages(),
            'messages': messages,
            'temp_context': temp_context,
            'variables': self.variables
        }

        self.history_manager.save_history(history_data)

    def current_variables(self):
        print("Попытка узнать переменные у персонажа без")
        return ""


class CrazyMita(Character):

    def __init__(self, name: str = "Mita", silero_command: str = "/speaker Mita"):

        """Добавляемые переменные"""
        self.attitude = 60
        self.boredom = 10
        self.stress = 5
        self.secretExposed = False
        self.secretExposedFirst = False

        super().__init__(name, silero_command)

    def init(self):
        self.crazy_mita_prompts()

    def crazy_mita_prompts(self):
        Prompts = []

        response_structure = load_text_from_file("Prompts/CrazyMitaPrompts/Structural/response_structure.txt")
        Prompts.append(PromptPart(PromptType.FIXED_START, response_structure))

        common = load_text_from_file("Prompts/CrazyMitaPrompts/Main/common.txt")
        Prompts.append(PromptPart(PromptType.FIXED_START, common, "common"))

        main = load_text_from_file("Prompts/CrazyMitaPrompts/Main/main.txt")
        Prompts.append(PromptPart(PromptType.FIXED_START, main, "main"))

        mainPlaying = load_text_from_file("Prompts/CrazyMitaPrompts/Main/mainPlaing.txt")
        mainCrazy = load_text_from_file("Prompts/CrazyMitaPrompts/Main/mainCrazy.txt")
        Prompts.append(PromptPart(PromptType.FIXED_START, mainPlaying, "mainPlaying", False))
        Prompts.append(PromptPart(PromptType.FIXED_START, mainCrazy, "mainCrazy", False))

        player = load_text_from_file("Prompts/CrazyMitaPrompts/Main/player.txt")
        Prompts.append(PromptPart(PromptType.FIXED_START, player))
        # Добавляем примеры длинных диалогов

        examplesLong = load_text_from_file("Prompts/CrazyMitaPrompts/Context/examplesLong.txt")
        examplesLongCrazy = load_text_from_file("Prompts/CrazyMitaPrompts/Context/examplesLongCrazy.txt")
        Prompts.append(PromptPart(PromptType.FIXED_START, examplesLong, "examplesLong"))
        Prompts.append(PromptPart(PromptType.FIXED_START, examplesLongCrazy, "examplesLongCrazy", False))

        #world = load_text_from_file("CrazyMitaPrompts/NotUsedNow/world.txt")
        #Prompts.append(PromptPart(PromptType.FIXED_START, world, "world"))

        mita_history = load_text_from_file("Prompts/CrazyMitaPrompts/Context/mita_history.txt")
        Prompts.append(PromptPart(PromptType.FIXED_START, mita_history, "mita_history", False))

        variableEffects = load_text_from_file("Prompts/CrazyMitaPrompts/Structural/VariablesEffects.txt")
        Prompts.append(PromptPart(PromptType.FIXED_START, variableEffects, "variableEffects"))

        SecretExposedText = load_text_from_file("Prompts/CrazyMitaPrompts/Events/SecretExposed.txt")
        Prompts.append(PromptPart(PromptType.FLOATING_SYSTEM, SecretExposedText, "SecretExposedText"))

        for prompt in Prompts:
            self.add_prompt_part(prompt)

    def safe_history(self, messages, temp_context):
        self.variables = {
            "attitude": self.attitude,
            "boredom": self.boredom,
            "stress": self.stress,
            "secret": self.secretExposed,
            "secret_first": self.secretExposedFirst
        }

        super().safe_history(messages, temp_context)

    def load_history(self):
        data = super().load_history()

        variables = data.get("variables")
        self.attitude = variables.get("attitude", self.attitude)
        self.boredom = variables.get("boredom", self.boredom)
        self.stress = variables.get("stress", self.stress)
        self.secretExposed = variables.get("secret", self.secretExposed)
        self.secretExposedFirst = variables.get("secret_first", self.secretExposedFirst)
        return data

    def process_logic(self, messages: dict):
        # Логика для поведения при игре с игроком
        if self.attitude < 50 and not (self.secretExposed or self.PlayingFirst):
            self._start_playing_with_player()

        # Логика для раскрытия секрета
        elif (self.attitude <= 10 or self.secretExposed) and not self.secretExposedFirst:
            self._reveal_secret(messages)

    def process_response(self, response: str):
        super().process_response(response)
        response = self._process_behavior_changes(response)
        response = self._detect_secret_exposure(response)
        return response

    def _start_playing_with_player(self):
        """Игровая логика, когда персонаж начинает играть с игроком"""
        print("Играет с игроком в якобы невиновную")
        self.PlayingFirst = True
        self.replace_prompt("main", "mainPlaying")

    def _reveal_secret(self, messages):
        """Логика раскрытия секрета"""
        print("Перестала играть вообще")
        self.secretExposedFirst = True
        self.secretExposed = True
        self.replace_prompt("main", "mainCrazy")
        self.replace_prompt("mainPlaying", "mainCrazy")
        self.replace_prompt("examplesLong", "examplesLongCrazy")

    def _process_behavior_changes(self, response):
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

        return response
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

    def _detect_secret_exposure(self, response):
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

    def current_variables(self):
        return {
            "role": "system",
            "content": (f"Твои характеристики:"
                        f"Отношение: {self.attitude}/100."
                        f"Скука: {self.boredom}/100."
                        f"Стресс: {self.stress}/100."
                        f"Состояние секрета: {self.secretExposed}")
        }

    def current_variables_string(self) -> str:
        characteristics = {
            "Отношение": self.attitude,
            "Стресс": self.stress,
            "Скука": self.boredom,
            "Состояние секрета": self.secretExposed,
        }
        return f"характеристики {self.name}:\n" + "\n".join(
            f"- {key}: {value}" for key, value in characteristics.items()
        )

class KindMita(Character):
    def init(self):
        self.kind_mita_prompts()

    def kind_mita_prompts(self):
        Prompts = []

        response_structure = load_text_from_file("Prompts/Kind/Structural/response_structure.txt")
        Prompts.append(PromptPart(PromptType.FIXED_START, response_structure))

        common = load_text_from_file("Prompts/Kind/Main/common.txt")
        Prompts.append(PromptPart(PromptType.FIXED_START, common, "common"))

        main = load_text_from_file("Prompts/Kind/Main/main.txt")
        Prompts.append(PromptPart(PromptType.FIXED_START, main, "main"))

        player = load_text_from_file("Prompts/Kind/Main/player.txt")
        Prompts.append(PromptPart(PromptType.FIXED_START, player))
        # Добавляем примеры длинных диалогов

        examplesLong = load_text_from_file("Prompts/Kind/Context/examplesLong.txt")
        Prompts.append(PromptPart(PromptType.FIXED_START, examplesLong, "examplesLong"))

        mita_history = load_text_from_file("Prompts/Kind/Context/mita_history.txt")
        Prompts.append(PromptPart(PromptType.FIXED_START, mita_history, "mita_history"))

        variableEffects = load_text_from_file("Prompts/Kind/Structural/VariablesEffects.txt")
        Prompts.append(PromptPart(PromptType.FIXED_START, variableEffects, "variableEffects"))

        for prompt in Prompts:
            self.add_prompt_part(prompt)


class CappyMita(Character):

    def init(self):
        self.cappy_mita_prompts()

    def cappy_mita_prompts(self):
        Prompts = []

        response_structure = load_text_from_file("Prompts/CrazyMitaPrompts/Structural/response_structure.txt")
        Prompts.append(PromptPart(PromptType.FIXED_START, response_structure))

        common = load_text_from_file("Prompts/CrazyMitaPrompts/Main/common.txt")
        Prompts.append(PromptPart(PromptType.FIXED_START, common, "common"))

        examplesLong = load_text_from_file("Prompts/Cappy/cappy_examples.txt")
        Prompts.append(PromptPart(PromptType.FIXED_START, examplesLong, "examplesLong"))

        mita_history = load_text_from_file("Prompts/Cappy/cappy_history.txt")
        Prompts.append(PromptPart(PromptType.FIXED_START, mita_history, "mita_history"))

        variableEffects = load_text_from_file("Prompts/CrazyMitaPrompts/Structural/VariablesEffects.txt")
        Prompts.append(PromptPart(PromptType.FIXED_START, variableEffects, "variableEffects"))

        for prompt in Prompts:
            self.add_prompt_part(prompt)


#region Cartridges
class Cartridge(Character):
    ...


class SpaceCartridge(Cartridge):

    def init(self):
        self.cart_space_prompts()

    def cart_space_prompts(self):
        Prompts = []

        response_structure = load_text_from_file("Prompts/Cartridges/space cartridge.txt")
        Prompts.append(PromptPart(PromptType.FIXED_START, response_structure))

        for prompt in Prompts:
            self.add_prompt_part(prompt)

#endregion
