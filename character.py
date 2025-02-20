from typing import Dict, List

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

        self.history_file = HistoryManager()
        self.memory_file = MemorySystem()

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
        print("Замена промпта")

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
            repeated_system_message += " Remember facts for 3 messages by using <+memory>high|The player attaked me</memory>"

        messages.append({"role": "system", "content": repeated_system_message})

        return messages

    def init(self):
        raise NotImplementedError("Метод init должен быть реализован в подклассе")

    def process_logic(self, messages: dict):
        """То, как должно что-то менсять до получения ответа"""
        print("Персонаж без изменяемой логики пропмтов")

    def process_response(self, messages: dict):
        """То, как должно что-то меняться в результате ответа"""

        print("Персонаж без изменяемой логики пропмтов")

class CrazyMita(Character):

    def __init__(self, name: str = "Mita", silero_command: str = "/speaker Mita"):
        super().__init__(name, silero_command)

        self.attitude = 60
        self.boredom = 10
        self.stress = 5

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
        Prompts.append(PromptPart(PromptType.FIXED_START, mita_history, "mita_history"))

        variableEffects = load_text_from_file("Prompts/CrazyMitaPrompts/Structural/VariablesEffects.txt")
        Prompts.append(PromptPart(PromptType.FIXED_START, variableEffects, "variableEffects"))

        SecretExposedText = load_text_from_file("Prompts/CrazyMitaPrompts/Events/SecretExposed.txt")
        Prompts.append(PromptPart(PromptType.FLOATING_SYSTEM, SecretExposedText, "SecretExposedText"))

        for prompt in Prompts:
            self.add_prompt_part(prompt)

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
        #add_temporary_system_message(messages, f"{self.SecretExposedText}")
        system_message = {"role": "system", "content": f"{self.mita_history}\n"}
        self.systemMessages.append(system_message)

        self.current_character.replace_prompt("main", "mainCrazy")
        self.current_character.replace_prompt("mainPlaying", "mainCrazy")


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
