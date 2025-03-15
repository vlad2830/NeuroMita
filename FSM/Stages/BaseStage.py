from abc import ABC, abstractmethod
from typing import Type
from FSM.BaseState import BaseState


class BaseStage(ABC):
    """Абстрактный базовый класс для этапов игры"""
    
    def __init__(self, available_state_classes: list[Type[BaseState]]):
        """
        Инициализация этапа
        :param available_state_classes: список доступных классов состояний для этого этапа
        """
        self._available_state_classes = available_state_classes

    def get_available_states(self) -> list[Type[BaseState]]:
        """
        Получить список доступных классов состояний
        :return: список доступных классов состояний
        """
        return self._available_state_classes
