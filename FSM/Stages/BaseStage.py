from abc import ABC

from BaseState import BaseState


class BaseStage(ABC):
    def __init__(self, available_states: list[BaseState]):
        self._available_states = available_states

    def get_available_states(self) -> list[BaseState]:
        return self._available_states
    
    