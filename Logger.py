import logging
import colorlog
import os

# Настройка фильтра для отображения только ваших логов
class MyFileFilter(logging.Filter):
    def filter(self, record):
        # Путь к вашей директории с проектом
        project_path = os.path.dirname(os.path.abspath(__file__))
        # Проверяем, что запись лога относится к вашему проекту
        if record.pathname.startswith(project_path):
            return True
        return False

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

# Добавляем фильтр к обработчику
handler.addFilter(MyFileFilter())

# Устанавливаем высокий уровень для корневого логгера
logging.basicConfig(level=logging.WARNING)

logger = colorlog.getLogger(__name__)
logger.setLevel(logging.INFO)
logger.addHandler(handler)
logger.propagate = False  # Отключаем распространение логов в корневой логгер