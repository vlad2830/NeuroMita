from typing import Dict, List, Optional
import re
from PromPart import PromptPart, PromptType
#from chat_model import ChatModel
from utils import load_text_from_file


class Character:
    def __init__(self, name: str, silero_command: str):
        self.name = name
        self.silero_command = silero_command

        self.fixed_prompts: List[PromptPart] = []
        self.float_prompts: List[PromptPart] = []
        self.temp_prompts: List[PromptPart] = []

    def add_prompt_part(self, part: PromptPart):
        if part.is_fixed:
            self.fixed_prompts.append(part)
        elif part.is_floating:
            self.float_prompts.append(part)
        elif part.is_temporary:
            self.temp_prompts.append(part)
        else:
            print("Добавляется неизвестный промпарт")

    def replace_prompt(self, name_current, name_next):
        self.fixed_prompts[name_current].active = False
        self.fixed_prompts[name_next].active = True

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
