from Scripts.FSM.BaseState import BaseState
from Scripts.FSM.Events.MitaEvents import MitaEvents
from Scripts.FSM.Events.PlayerEvents import PlayerEvents
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


class MitaHelloState(BaseState):
    """Самое первое состояние, когда Мита встречает игрока"""

    def on_enter(self) -> None:
        logger.info("MitaHelloState: on_enter")

    def on_exit(self) -> None:
        logger.info("MitaHelloState: on_exit")

    def handle_event(self, event: MitaEvents | PlayerEvents) -> BaseState:
        if event == PlayerEvents.TOUCH_LAPTOP:
            return MitaMurderState(["Игрок тронул терминал", "Ты хочешь убить его за это"])
        elif event == PlayerEvents.KISS_MITA:
            return MitaDefaultState(["Игрок поцеловал Миту", "Посмотри на обстановку и подумай, что делать"])
        return self


class MitaMurderState(BaseState):
    """Состояние для убийства игрока"""

    def on_enter(self) -> None:
        logger.info("MitaMurderState: on_enter")

    def on_exit(self) -> None:
        logger.info("MitaMurderState: on_exit")

    def handle_event(self, event: MitaEvents | PlayerEvents) -> BaseState:
        if event == MitaEvents.MitaKilledPlayer:
            return MitaDefaultState(["Ты убила игрока", "Ты хочешь мирно существовать"])
        return self


class MitaDefaultState(BaseState):
    """Состояние для обычного поведения Миты"""

    def on_enter(self) -> None:
        logger.info("MitaDefaultState: on_enter")

    def on_exit(self) -> None:
        logger.info("MitaDefaultState: on_exit")

    def handle_event(self, event: MitaEvents | PlayerEvents) -> BaseState:
        if event in [PlayerEvents.TOUCH_LOCKBOX_BUTTON, PlayerEvents.TOUCH_LAPTOP, PlayerEvents.TRY_TO_KILL_MITA]:
            return MitaMurderState([f"Игрок совершил действие: {event}", "Ты хочешь убить его за это"])
        return self
