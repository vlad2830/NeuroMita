from Events.MitaEvents import MitaEvents
from Events.PlayerEvents import PlayerEvents
from loguru import logger
from BaseState import BaseState

class FiniteStateMachine:

    def __init__(self, initial_state: BaseState):
        self.current_state = initial_state

    async def _set_state(self, state: BaseState) -> None:
        """Выполнить переход к указанному состоянию"""
        logger.info(f"Выходим из состояния: {self.current_state.__class__.__name__}")
        await self.current_state.on_exit()

        self.current_state = state

        logger.info(f"Входим в состояние: {self.current_state.__class__.__name__}")
        await self.current_state.on_enter()
        logger.info(f"Выполнен переход в состояние: {self.current_state.__class__.__name__}")
    async def handle_event(self, event: MitaEvents | PlayerEvents) -> None:
        """Обработать событие"""
        new_state = await self.current_state.handle_event(event)
        if new_state is not None and new_state != self.current_state:
            logger.info(f"Переход из {self.current_state.__class__.__name__} в {new_state.__class__.__name__}")
            await self._set_state(new_state)


