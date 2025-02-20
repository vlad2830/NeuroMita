from typing import Dict, List, Optional
import re
from promptPart import PromptPart, PromptType
from utils import load_text_from_file


class Character:
    def __init__(self, name: str, silero_command: str):
        self.name = name
        self.silero_command = silero_command

        self.fixed_prompts: List[PromptPart] = []
        self.float_prompts: List[PromptPart] = []
        self.temp_prompts: List[PromptPart] = []
        self.events: List[PromptPart] = []

        self.variables = dict()

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
        messages = []

        for part in self.fixed_prompts:
            if part.active:
                m = {"role": "system", "content": str(part)}
                messages.append(m)

        return messages


def crazy_mita_prompts(mita_character: Character, chat_model=None):
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
        mita_character.add_prompt_part(prompt)

def kind_mita_prompts(mita_character: Character, chat_model=None):
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
        mita_character.add_prompt_part(prompt)



def cappy_mita_prompts(mita_character: Character, chat_model=None):
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
        mita_character.add_prompt_part(prompt)


def cart_space_prompts(mita_character: Character, chat_model=None):
    Prompts = []

    response_structure = load_text_from_file("Prompts/Cartridges/space cartridge.txt")
    Prompts.append(PromptPart(PromptType.FIXED_START, response_structure))

    for prompt in Prompts:
        mita_character.add_prompt_part(prompt)
