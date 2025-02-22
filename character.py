from typing import Dict, List

from EventState import EventState
from MemorySystem import MemorySystem
from promptPart import PromptPart, PromptType
from HistoryManager import HistoryManager
from utils import load_text_from_file
import datetime


class Character:
    def __init__(self, name: str, silero_command: str):

        self.name = name
        self.silero_command = silero_command

        self.fixed_prompts: List[PromptPart] = []
        self.float_prompts: List[PromptPart] = []
        self.temp_prompts: List[PromptPart] = []
        self.events: List[PromptPart] = []
        self.variables = []

        self.history_file = HistoryManager(self.name)
        self.load_history()

        self.memory_file = MemorySystem()
        self.state = EventState()

        """
        Спорные временные переменные
        """
        self.LongMemoryRememberCount = 0

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

    def process_response(self, messages: dict):
        """То, как должно что-то меняться в результате ответа"""
        return messages
        print("Персонаж без изменяемой логики пропмтов")

    def load_history(self):
        """Кастомная обработка загрузки истории"""
        return self.history_file.load_history()

    def safe_history(self, messages: dict, temp_context: dict):
        """Кастомная обработка сохранения истории"""

        history_data = {
            'fixed_parts': self.prepare_fixed_messages(),
            'messages': messages,
            'temp_context': temp_context,
            'variables': self.variables
        }

        self.history_file.save_history(history_data)

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

        self.attitude = data.get("attitude", self.attitude)
        self.boredom = data.get("boredom", self.boredom)
        self.stress = data.get("stress", self.stress)
        self.secretExposed = data.get("secret", self.secretExposed)
        self.secretExposedFirst = data.get("secret_first", self.secretExposedFirst)
        return data

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

    def current_variables(self):
        return {
            "role": "system",
            "content": (f"Твои характеристики:"
                        f"Отношение: {self.attitude}/100."
                        f"Стресс: {self.stress}/100."
                        f"Скука: {self.boredom}/100."
                        f"Состояние секрета: {self.secretExposed}")
        }


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
