from abc import ABC, abstractmethod

from Events.MitaEvents import MitaEvents
from Events.PlayerEvents import PlayerEvents


class BaseState(ABC):
    """Абстрактный класс для состояний"""

    @abstractmethod
    async def on_enter(self) -> None:
        """Вызывается при входе в состояние"""
        pass

    @abstractmethod
    async def on_exit(self) -> None:
        """Вызывается при выходе из состояния"""
        pass

    @abstractmethod
    async def handle_event(self, event: MitaEvents | PlayerEvents) -> 'BaseState':
        # if event == MitaEvents.StartHunt:
        #     return MitaMurderState()

        """Обработка событий, возвращает State"""
        pass