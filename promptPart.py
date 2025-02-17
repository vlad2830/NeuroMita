from typing import Dict, List, Optional
import enum


class PromptType(enum.Enum):
    """Типы промптов."""
    FIXED_START = 1  # Фиксированный в начале
    FLOATING_SYSTEM = 2  # Плавающий системный
    CONTEXT_TEMPORARY = 3  # Временный контекстный
    EVENT = 4  # Временный контекстный


class PromptPart:
    """Класс для представления части промпта."""

    def __init__(self, type: PromptType, text: str, name=None, active=True,parameters: Optional[Dict] = None):
        """
        Инициализация части промпта.

        :param part_type: Тип промпта (из PromptType).
        :param text: Содержимое промпта.
        :param parameters: Параметры для форматирования (опционально).
        """
        self.name = name
        self.type = type
        self.text = text
        self.active = active
        self.parameters = parameters or {}

    def format(self, **kwargs) -> str:
        """Форматирует содержимое с параметрами как f-строка."""
        try:
            return self.text.format(**{**self.parameters, **kwargs})
        except KeyError as e:
            raise ValueError(f"Отсутствует параметр {e} в шаблоне промпта")

    @property
    def is_fixed(self) -> bool:
        """Проверяет, является ли промпт фиксированным в начале."""
        return self.type == PromptType.FIXED_START

    @property
    def is_floating(self) -> bool:
        """Проверяет, является ли промпт плавающим системным."""
        return self.type == PromptType.FLOATING_SYSTEM

    @property
    def is_temporary(self) -> bool:
        """Проверяет, является ли промпт временным контекстным."""
        return self.type == PromptType.CONTEXT_TEMPORARY

    @property
    def is_event(self) -> bool:
        """Проверяет, является ли промпт временным контекстным."""
        return self.type == PromptType.EVENT

    def __str__(self) -> str:
        """Строковое представление объекта для удобного вывода."""
        return self.text
