from BaseState import BaseState
from Events.MitaEvents import MitaEvents
from Events.PlayerEvents import PlayerEvents
#region Logging
# Настройка логирования
import logging
import colorlog

# Настройка цветного логирования
handler = colorlog.StreamHandler()
handler.setFormatter(colorlog.ColoredFormatter(
    '%(log_color)s%(asctime)s - %(levelname)s - %(message)s',
    log_colors={
        'INFO': 'white',
        'WARNING': 'yellow',
        'ERROR': 'red',
        'CRITICAL': 'red,bg_white',
    }
))

logger = colorlog.getLogger(__name__)
logger.setLevel(logging.INFO)
logger.addHandler(handler)
#endregion


class MitaMurderState(BaseState):
    """Состояние для убийства игрока"""

    async def on_enter(self) -> None:
        # await MitaAction.execute()
        logger.info("MitaMurderState: on_enter")

    async def on_exit(self) -> None:
        # await MitaAction.execute()
        logger.info("MitaMurderState: on_exit")

    async def handle_event(self, event: MitaEvents | PlayerEvents) -> BaseState:
        if event == MitaEvents.MitaKilledPlayer:
            return MitaDefaultState()
        return self


class MitaDefaultState(BaseState):
    """Состояние для обычного поведения Миты"""

    async def on_enter(self) -> None:
        # await MitaAction.execute()
        logger.info("MitaDefaultState: on_enter")

    async def on_exit(self) -> None:
        # await MitaAction.execute()
        logger.info("MitaDefaultState: on_exit")

    async def handle_event(self, event: MitaEvents | PlayerEvents) -> BaseState:
        if event in [PlayerEvents.TOUCH_LOCKBOX_BUTTON, PlayerEvents.TOUCH_LAPTOP, PlayerEvents.TRY_TO_KILL_MITA]:
            return MitaMurderState()
        return self
