from typing import Dict, List, Optional
import re
from PromPart import PromptPart, PromptType
from utils import load_text_from_file


class Character:
    def __init__(self, name, silero_command, memory_limit: int = 40):
        self.name = name
        self.silero_command = silero_command
        self.prompt_parts: List[PromptPart] = []
        self.memory_limit = memory_limit
        self._message_history: List[Dict] = []

    def add_prompt_part(self, part: PromptPart):
        self.prompt_parts.append(part)

    def prepare_messages(self, current_context: Dict, temporary_prompts: List[Dict] = None) -> List[Dict]:
        messages = []
        temporary = temporary_prompts or []

        # 1. Фиксированные в начале
        fixed = [p for p in self.prompt_parts if p.is_fixed_start]
        messages.extend({
                            "role": "system",
                            "content": p.format(**current_context)
                        } for p in fixed)

        # 2. Плавающие системные (из истории)
        floating_history = [msg for msg in self._message_history if self._is_floating_system(msg)]
        messages.extend(floating_history[-self.memory_limit:])

        # 3. Временные контекстные (не сохраняются)
        context_temp = [p for p in self.prompt_parts if p.is_context_temporary]
        messages.extend({
                            "role": "system",
                            "content": p.format(**current_context)
                        } for p in context_temp)

        # Добавляем временные промпты из аргументов
        messages.extend(temporary)

        return messages

    def update_history(self, new_messages: List[Dict]):
        """Обновляет историю сообщений"""
        self._message_history.extend(new_messages)

        # Ограничиваем историю по количеству плавающих сообщений
        floating = [msg for msg in self._message_history if self._is_floating_system(msg)]
        if len(floating) > self.memory_limit:
            self._message_history = floating[-self.memory_limit:]

    def _is_floating_system(self, message: Dict) -> bool:
        """Определяет плавающее системное сообщение по паттерну"""
        if message['role'] != 'system':
            return False

        content = message.get('content', '')
        return bool(re.search(r'\[FLOATING\]', content))


def InitMita(mita_character: Character):
    common = load_text_from_file("Promts/Main/common.txt")
    main = load_text_from_file("Promts/Main/main.txt")
    player = load_text_from_file("Promts/Main/player.txt")
    mainPlaying = load_text_from_file("Promts/Main/mainPlaing.txt")
    mainCrazy = load_text_from_file("Promts/Main/mainCrazy.txt")

    examplesLong = load_text_from_file("Promts/Context/examplesLong.txt")
    examplesLongCrazy = load_text_from_file("Promts/Context/examplesLongCrazy.txt")

    world = load_text_from_file("Promts/NotUsedNow/world.txt")
    mita_history = load_text_from_file("Promts/Context/mita_history.txt")

    variableEffects = load_text_from_file("Promts/Structural/VariablesEffects.txt")
    response_structure = load_text_from_file("Promts/Structural/response_structure.txt")

    SecretExposedText = load_text_from_file("Promts/Events/SecretExposed.txt")

    prompt = PromptPart(PromptType.FIXED_START, "ААА")
    mita_character.add_prompt_part(prompt)
