# GpuUtils.py
import subprocess
import platform
import os
import re # Импортируем модуль для регулярных выражений



def check_gpu_provider():
    """
    Проверяет вендора GPU (NVIDIA или AMD) в системе Windows.
    Возвращает:
        str: "NVIDIA", "AMD" или None, если не удалось определить или не Windows.
    """
    if platform.system() != "Windows":
        print("Предупреждение: Определение вендора GPU реализовано только для Windows.")
        return None # Возвращаем None для не-Windows систем

    try:
        output = subprocess.check_output(
            "wmic path win32_VideoController get name",
            shell=True, text=True, stdin=subprocess.DEVNULL, stderr=subprocess.PIPE
        ).strip()

        amd_test = os.environ.get('TEST_AS_AMD', '').upper() == 'TRUE'

        if "NVIDIA" in output:
            return "NVIDIA" if not amd_test else "AMD" 
        elif "AMD" in output or "Radeon" in output:
            return "AMD"
        else:
            print(f"Не удалось определить вендора GPU из вывода WMI:\n{output}")
            return None
    except FileNotFoundError:
        print("Ошибка: Команда 'wmic' не найдена. Убедитесь, что вы запускаете скрипт в Windows.")
        return None
    except subprocess.CalledProcessError as e:
        print(f"Ошибка при выполнении команды wmic: {e}")
        print(f"Stderr: {e.stderr}")
        return None
    except Exception as e:
        print(f"Неожиданная ошибка при определении GPU: {e}")
        return None

def get_cuda_devices():
    cuda_devices = []
    try:
        # print("Попытка импорта PyTorch для определения CUDA устройств...") # Убрал вывод
        import torch
        # print(f"PyTorch импортирован. Версия: {torch.__version__}") # Убрал вывод

        if torch.cuda.is_available():
            # print("CUDA доступен в PyTorch.") # Убрал вывод
            device_count = torch.cuda.device_count()
            # print(f"Найдено CUDA устройств: {device_count}") # Убрал вывод
            if device_count > 0:
                for i in range(device_count):
                    # device_name = torch.cuda.get_device_name(i) # Получение имени здесь не нужно для списка ID
                    cuda_devices.append(f"cuda:{i}")
                    # print(f"  - Обнаружено: cuda:{i} ({device_name})") # Убрал вывод
            # else: # Убрал вывод
                # print("torch.cuda.is_available() вернул True, но device_count <= 0. Странная ситуация.")
        # else: # Убрал вывод
            # print("CUDA недоступен в установленной версии PyTorch.")
            # try:
            #     smi_output = subprocess.check_output(["nvidia-smi", "-L"], text=True, stderr=subprocess.STDOUT)
            #     print("Вывод 'nvidia-smi -L':")
            #     print(smi_output)
            #     print("NVIDIA драйверы установлены, но PyTorch не видит CUDA. Возможные причины:")
            #     print("- PyTorch установлен для CPU (pip install torch), а не для CUDA.")
            #     print("- Несовместимость версии CUDA драйвера и CUDA Toolkit, с которым собран PyTorch.")
            #     print("- Проблемы с окружением (переменные PATH, CUDA_VISIBLE_DEVICES).")
            # except (FileNotFoundError, subprocess.CalledProcessError, Exception) as smi_e:
            #     print(f"Не удалось выполнить 'nvidia-smi -L' для диагностики: {smi_e}")
            #     print("Возможно, NVIDIA драйверы не установлены или nvidia-smi не в PATH.")

    except ImportError:
        print("PyTorch не найден. Невозможно определить CUDA устройства через PyTorch.")
        # print("Для автоматического определения всех GPU NVIDIA рекомендуется установить PyTorch с поддержкой CUDA.") # Убрал вывод
    except Exception as e:
        print(f"Неожиданная ошибка при проверке CUDA устройств через PyTorch: {e}")

    # if not cuda_devices: # Убрал вывод
        # print("Не удалось определить CUDA устройства через PyTorch.")

    return cuda_devices

def get_gpu_name_by_id(device_id):
    """
    Получает имя GPU по его ID (например, 'cuda:0').
    Требует установленный PyTorch с поддержкой CUDA.

    Args:
        device_id (str): Идентификатор устройства, например 'cuda:0'.

    Returns:
        str: Имя GPU или None, если не удалось определить.
    """
    if not isinstance(device_id, str) or not device_id.startswith("cuda:"):
        # print(f"Неверный формат device_id: {device_id}. Ожидается 'cuda:X'.") # Убрал вывод
        return None

    try:
        # Извлекаем индекс из строки 'cuda:X'
        match = re.match(r"cuda:(\d+)", device_id)
        if not match:
            # print(f"Не удалось извлечь индекс из device_id: {device_id}") # Убрал вывод
            return None
        index = int(match.group(1))

        import torch
        if torch.cuda.is_available() and index < torch.cuda.device_count():
            return torch.cuda.get_device_name(index)
        else:
            # print(f"CUDA недоступен или индекс {index} вне диапазона доступных устройств.") # Убрал вывод
            return None
    except ImportError:
        print("PyTorch не найден. Невозможно получить имя GPU.")
        return None
    except Exception as e:
        print(f"Ошибка при получении имени GPU для {device_id}: {e}")
        return None