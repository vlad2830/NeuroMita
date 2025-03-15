from Events.MitaEvents import MitaEvents
from Events.PlayerEvents import PlayerEvents
from BaseState import BaseState
#region Logging
# Настройка логирования
import logging
import colorlog

from Stages.BaseStage import BaseStage

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


class FiniteStateMachine:

    def __init__(self, initial_state: BaseState, initial_stage: BaseStage):
        self.current_state = initial_state
        self.current_stage = initial_stage

    def _set_state(self, state: BaseState) -> None:
        """Выполнить переход к указанному состоянию"""
        logger.info(f"Выходим из состояния: {self.current_state.__class__.__name__}")
        self.current_state.on_exit()

        self.current_state = state

        logger.info(f"Входим в состояние: {self.current_state.__class__.__name__}")
        self.current_state.on_enter()
        logger.info(f"Выполнен переход в состояние: {self.current_state.__class__.__name__}")

    def handle_event(self, event: MitaEvents | PlayerEvents) -> None:
        """Обработать событие"""
        new_state = self.current_state.handle_event(event)
        if new_state is not None and new_state != self.current_state:
            if not new_state.__class__ in self.current_stage.get_available_states():
                logger.error(f"{new_state.__class__.__name__} не входит в допустимые состояния для этапа {self.current_stage.__class__.__name__}")
                return
            logger.info(f"Переход из {self.current_state.__class__.__name__} в {new_state.__class__.__name__}")
            self._set_state(new_state)

    def process_logic(self, message_text):
        self.current_state.process_logic(message_text)

    def get_prompts_text(self):
        self.current_state.get_prompts_text()
