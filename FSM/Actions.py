from abc import ABC, abstractmethod


class BaseAction(ABC):
    """Базовый класс для действий"""

    @abstractmethod
    async def execute(self) -> None:
        """Выполнение действия"""
        pass


class MitaKillPlayerAction(BaseAction):
    """Действие для убийства игрока"""

    async def execute(self) -> None:
        # запрос на c# чтобы мита тпхнулась к игроку и мгновенно убила его
        pass

