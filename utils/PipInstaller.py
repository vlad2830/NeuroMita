import subprocess
import sys
import os
import queue
import threading
import time
import tkinter as tk

class PipInstaller:
    def __init__(self, script_path, libs_path="Lib", update_status=None, update_log=None, progress_window=None):
        self.script_path = script_path
        self.libs_path = libs_path
        self.libs_path_abs = os.path.abspath(libs_path)
        # Функции обратного вызова для обновления GUI (или просто print, если GUI нет)
        self.update_status = update_status if update_status else lambda msg: print(f"Status: {msg}")
        self.update_log = update_log if update_log else lambda msg: print(f"Log: {msg}")
        self.progress_window = progress_window # Для проверки существования окна в _run_pip_process

        self._ensure_libs_path()

    def _ensure_libs_path(self):
        """Проверяет/создает папку Lib и добавляет её в sys.path."""
        if not os.path.exists(self.libs_path):
            try:
                os.makedirs(self.libs_path)
                self.update_log(f"Создана директория: {self.libs_path}")
            except OSError as e:
                self.update_log(f"Ошибка при создании директории {self.libs_path}: {e}")
                raise # Передаем ошибку дальше, т.к. без папки установка невозможна

        if self.libs_path_abs not in sys.path:
            sys.path.insert(0, self.libs_path_abs)
            self.update_log(f"Добавлен путь {self.libs_path_abs} в sys.path")

    def install_package(self, package_spec, description="Установка пакета...", extra_args=None):
        """
        Устанавливает один или несколько пакетов с помощью pip.

        Args:
            package_spec (str or list): Имя пакета, URL или список имен/URL.
            description (str): Описание для отображения статуса.
            extra_args (list, optional): Дополнительные аргументы для pip (например, --upgrade, --index-url).

        Returns:
            bool: True в случае успеха, False в случае ошибки.
        """
        base_cmd = [
            self.script_path,
            "-m",
            "pip",
            "install",
            "--target",
            self.libs_path_abs,
            "--no-user",
            "--no-cache-dir",
            # "--use-pep517", # Можно раскомментировать при проблемах со сборкой
            # "--no-build-isolation" # Можно раскомментировать при проблемах со сборкой
        ]

        if extra_args:
            base_cmd.extend(extra_args)

        if isinstance(package_spec, list):
            base_cmd.extend(package_spec)
        else:
            base_cmd.append(package_spec)

        return self._run_pip_process(base_cmd, description)

    def _run_pip_process(self, cmd, description):
        """Запускает процесс pip, обрабатывает вывод и таймауты."""
        self.update_status(description)
        self.update_log(f"Выполняем: {' '.join(cmd)}")

        try:
            process = subprocess.Popen(
                cmd,
                stdout=subprocess.PIPE,
                stderr=subprocess.PIPE,
                universal_newlines=True,
                encoding='utf-8',
                errors='ignore',
                bufsize=1,
                creationflags=subprocess.CREATE_NO_WINDOW
            )
        except FileNotFoundError:
            self.update_log(f"ОШИБКА: Не найден исполняемый файл Python: {self.script_path}")
            self.update_status("Ошибка: Python не найден")
            return False
        except Exception as e:
            self.update_log(f"ОШИБКА при запуске subprocess: {e}")
            self.update_status("Ошибка запуска процесса")
            return False


        stdout_queue = queue.Queue()
        stderr_queue = queue.Queue()

        def read_output(pipe, queue_obj):
            try:
                for line in iter(pipe.readline, ''):
                    if line:
                        queue_obj.put(line.strip())
            except Exception as e:
                # Можно логировать ошибку чтения потока, если нужно
                # self.update_log(f"Ошибка чтения потока: {e}")
                pass
            finally:
                try:
                    pipe.close()
                except OSError:
                    pass # Поток уже может быть закрыт

        stdout_thread = threading.Thread(target=read_output, args=(process.stdout, stdout_queue), daemon=True)
        stderr_thread = threading.Thread(target=read_output, args=(process.stderr, stderr_queue), daemon=True)
        stdout_thread.start()
        stderr_thread.start()

        def process_queues():
            msgs_processed = 0
            while not stdout_queue.empty():
                try:
                    line = stdout_queue.get_nowait()
                    # Фильтруем менее важные сообщения pip
                    # if not line.startswith("Requirement already satisfied:") and \
                    #    not line.startswith("Collecting ") and \
                    #    not line.startswith("Using cached ") and \
                    #    not line.startswith("Downloading ") and \
                    #    "  -" not in line: # Убираем тире отступы
                    if not line.startswith("Requirement already satisfied:") and \
                        not line.startswith("Collecting ") and \
                        not line.startswith("Using cached ") and \
                        "  -" not in line: # Убираем тире отступы
                        self.update_log(line)
                    msgs_processed += 1
                except queue.Empty:
                    break

            while not stderr_queue.empty():
                try:
                    line = stderr_queue.get_nowait()
                    # Логируем предупреждения и ошибки отдельно
                    if "WARNING:" in line.upper() or "DEPRECATION:" in line.upper():
                         self.update_log(f"INFO: {line}")
                    elif "ERROR:" in line.upper():
                        self.update_log(f"ОШИБКА: {line}")
                    else:
                        if line.strip():
                            self.update_log(f"STDERR: {line}")
                    msgs_processed += 1
                except queue.Empty:
                    break
            return msgs_processed

        start_time = time.time()
        last_activity_time = start_time
        timeout = 7200 # 120 минут
        no_activity_timeout = 1800 # 30 минут бездействия

        while process.poll() is None:
            if self.progress_window and not self.progress_window.winfo_exists():
                self.update_log("Окно прогресса закрыто, прерываем процесс установки.")
                if process.poll() is None: 
                    process.terminate()
                    time.sleep(0.5)
                    if process.poll() is None: process.kill()
                return False # Прерываем выполнение

            msgs_processed = process_queues()

            if msgs_processed > 0:
                last_activity_time = time.time()

            # Проверка на зависание
            if time.time() - last_activity_time > no_activity_timeout:
                self.update_log(f"Предупреждение: Процесс установки неактивен > {no_activity_timeout // 60} мин. Принудительное завершение.")
                if process.poll() is None:
                    process.terminate(); time.sleep(1)
                    if process.poll() is None: process.kill()
                return False

            if time.time() - start_time > timeout:
                self.update_log(f"Превышено максимальное время установки (> {timeout // 60} мин). Принудительное завершение.")
                if process.poll() is None:
                    process.terminate(); time.sleep(1)
                    if process.poll() is None: process.kill()
                return False

            if self.progress_window and self.progress_window.winfo_exists():
                try:
                    self.progress_window.update()
                except tk.TclError:
                    self.update_log("Окно прогресса было закрыто во время ожидания.")
                    if process.poll() is None: process.terminate(); time.sleep(0.5); process.kill()
                    return False
            time.sleep(0.1) # Небольшая пауза

        stdout_thread.join(timeout=5)
        stderr_thread.join(timeout=5)

        process_queues()

        returncode = process.returncode
        self.update_log(f"Pip завершился с кодом: {f"Успех {returncode}" if returncode == 0 else f"Ошибка {returncode}"}")

        if returncode != 0:
            self.update_log(f"Процесс pip завершился с ошибкой.")
            return False
        return True
