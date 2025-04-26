# PipInstaller.py
import subprocess
import sys
import os
import queue
import threading
import time
import tkinter as tk
import json


from packaging.requirements import Requirement
from packaging.utils import canonicalize_name, NormalizedName
from packaging.version import parse as parse_version
from Logger import logger

class DependencyResolver:
    def __init__(self, libs_path_abs, update_log_func):
        self.libs_path = libs_path_abs
        self.update_log = update_log_func
        self.cache_file_path = os.path.join(self.libs_path, "dependency_cache.json")
        self._dist_info_cache = {}
        self._dep_cache = {} # Кэш прямых зависимостей для текущего запуска
        self._tree_cache = self._load_tree_cache() # Кэш полных деревьев из файла

    def _get_package_version(self, package_name_canon: NormalizedName) -> str | None:
        dist_info_path = self._find_dist_info_path(package_name_canon)
        if not dist_info_path:
            return None
        # Версия обычно есть в имени папки .dist-info
        # Пример: numpy-1.23.5.dist-info
        try:
            parts = os.path.basename(dist_info_path).split('-')
            if len(parts) >= 2 and parts[-1] == "dist-info":
                version_str = parts[-2]
                # Простая проверка, что это похоже на версию
                if version_str and version_str[0].isdigit():
                    # Нормализуем версию на всякий случай
                    return str(parse_version(version_str))
        except Exception:
            pass # Ошибка парсинга версии

        # Если не нашли в имени, попробуем из METADATA (менее надежно)
        metadata_path = os.path.join(dist_info_path, "METADATA")
        if os.path.exists(metadata_path):
            try:
                with open(metadata_path, 'r', encoding='utf-8') as f:
                    for line in f:
                        if line.lower().startswith("version:"):
                            return line.split(":", 1)[1].strip()
            except Exception:
                pass
        return None

    def _load_tree_cache(self):
        if os.path.exists(self.cache_file_path):
            try:
                with open(self.cache_file_path, 'r', encoding='utf-8') as f:
                    # Простая загрузка, без блокировок, т.к. читаем один раз при инициализации
                    return json.load(f)
            except (json.JSONDecodeError, IOError) as e:
                self.update_log(f"Предупреждение: Не удалось загрузить кэш зависимостей: {e}")
        return {}

    def _save_tree_cache(self):
        try:
            # Простая запись, без блокировок, т.к. вызывается редко
            with open(self.cache_file_path, 'w', encoding='utf-8') as f:
                json.dump(self._tree_cache, f, indent=4)
        except IOError as e:
            self.update_log(f"Ошибка сохранения кэша зависимостей: {e}")

    def _find_dist_info_path(self, package_name_canon: NormalizedName):
        if package_name_canon in self._dist_info_cache:
            return self._dist_info_cache[package_name_canon]
        if not os.path.exists(self.libs_path): return None
        for item in os.listdir(self.libs_path):
            if item.endswith(".dist-info"):
                try:
                    dist_name = item.split('-')[0]
                    if canonicalize_name(dist_name) == package_name_canon:
                        path = os.path.join(self.libs_path, item)
                        self._dist_info_cache[package_name_canon] = path
                        return path
                except Exception: continue
        self._dist_info_cache[package_name_canon] = None
        return None

    def _get_direct_dependencies(self, package_name_canon: NormalizedName):
        if package_name_canon in self._dep_cache:
            return self._dep_cache[package_name_canon]
        dependencies = set()
        dist_info_path = self._find_dist_info_path(package_name_canon)
        if not dist_info_path:
            self._dep_cache[package_name_canon] = dependencies
            return dependencies
        metadata_path = os.path.join(dist_info_path, "METADATA")
        if not os.path.exists(metadata_path):
            self._dep_cache[package_name_canon] = dependencies
            return dependencies
        try:
            with open(metadata_path, 'r', encoding='utf-8') as f:
                for line in f:
                    if line.lower().startswith("requires-dist:"):
                        req_str = line.split(":", 1)[1].strip()
                        try:
                            req_part = req_str.split(';')[0].strip()
                            if req_part: dependencies.add(canonicalize_name(Requirement(req_part).name))
                        except Exception: pass
        except Exception: pass
        self._dep_cache[package_name_canon] = dependencies
        return dependencies

    def get_dependency_tree(self, root_package_name: str) -> set[NormalizedName]:
        root_canon = canonicalize_name(root_package_name)
        installed_version = self._get_package_version(root_canon)

        if not installed_version:
            self.update_log(f"Пакет '{root_package_name}' не найден в Lib, дерево зависимостей не может быть построено.")
            # Удаляем из кэша, если он там был с другой версией
            if root_canon in self._tree_cache:
                del self._tree_cache[root_canon]
                self._save_tree_cache()
            return set()

        # Проверяем кэш
        cached_entry = self._tree_cache.get(root_canon)
        if cached_entry and cached_entry.get("version") == installed_version:
            self.update_log(f"Используется кэшированное дерево зависимостей для {root_canon}=={installed_version}")
            return set(cached_entry.get("dependencies", []))

        # Строим дерево, если кэш неактуален или отсутствует
        self.update_log(f"Построение дерева зависимостей для {root_canon}=={installed_version}...")
        required_set = {root_canon}
        queue = [root_canon]
        processed = set()
        self._dist_info_cache = {} # Сбрасываем кэш путей для нового расчета
        self._dep_cache = {}      # Сбрасываем кэш прямых зависимостей

        while queue:
            current_pkg_canon = queue.pop(0)
            if current_pkg_canon in processed: continue
            processed.add(current_pkg_canon)
            direct_deps = self._get_direct_dependencies(current_pkg_canon)
            for dep_canon in direct_deps:
                if dep_canon not in required_set:
                    required_set.add(dep_canon)
                    if dep_canon not in processed: queue.append(dep_canon)

        # Сохраняем в кэш
        self._tree_cache[root_canon] = {
            "version": installed_version,
            "dependencies": sorted(list(required_set)) # Сохраняем как список для JSON
        }
        self._save_tree_cache()
        self.update_log(f"Дерево зависимостей для {root_canon} построено и закэшировано.")
        return required_set

    def get_all_installed_packages(self) -> set[NormalizedName]:
        installed_set = set()
        if not os.path.exists(self.libs_path): return installed_set
        for item in os.listdir(self.libs_path):
            if item.endswith(".dist-info"):
                try: installed_set.add(canonicalize_name(item.split('-')[0]))
                except Exception: pass
        return installed_set

class PipInstaller:
    def __init__(self, script_path, libs_path="Lib", update_status=None, update_log=None, progress_window=None):
        self.script_path = script_path
        self.libs_path = libs_path
        self.libs_path_abs = os.path.abspath(libs_path)
        self.update_status = update_status if update_status else lambda msg: logger.info(f"Status: {msg}")
        self.update_log = update_log if update_log else lambda msg: logger.info(f"Log: {msg}")
        self.progress_window = progress_window
        self._ensure_libs_path()

    def _ensure_libs_path(self):
        if not os.path.exists(self.libs_path):
            try: os.makedirs(self.libs_path); self.update_log(f"Создана директория: {self.libs_path}")
            except OSError as e: self.update_log(f"Ошибка создания {self.libs_path}: {e}"); raise
        if self.libs_path_abs not in sys.path:
            sys.path.insert(0, self.libs_path_abs); self.update_log(f"Добавлен путь {self.libs_path_abs} в sys.path")

    def install_package(self, package_spec, description="Установка пакета...", extra_args=None):
        self._ensure_libs_path()
        base_cmd = [self.script_path, "-m", "pip", "install", "--target", self.libs_path_abs, "--no-user", "--no-cache-dir"]
        if extra_args: base_cmd.extend(extra_args)
        if isinstance(package_spec, list): base_cmd.extend(package_spec)
        else: base_cmd.append(package_spec)
        install_success = self._run_pip_process(base_cmd, description)
        if not install_success: self.update_log(f"Установка пакета '{package_spec}' не удалась.")
        return install_success

    def uninstall_packages(self, packages_to_uninstall: list[str], description="Удаление пакетов..."):
        if not packages_to_uninstall:
            self.update_log("Список пакетов для удаления пуст.")
            return True
        self._ensure_libs_path()
        # Передаем оригинальные имена, pip должен их понять
        base_cmd = [self.script_path, "-m", "pip", "uninstall", "--yes"]
        base_cmd.extend(packages_to_uninstall)
        self.update_log(f"Попытка удаления пакетов: {', '.join(packages_to_uninstall)}")
        success = self._run_pip_process(base_cmd, description)
        if not success: self.update_log(f"Ошибка во время pip uninstall для: {', '.join(packages_to_uninstall)}")
        else: self.update_log(f"Команда pip uninstall для '{', '.join(packages_to_uninstall)}' завершена.")
        return success

    def _run_pip_process(self, cmd, description):
        self.update_status(description)
        self.update_log(f"Выполняем: {' '.join(cmd)}")
        try:
            process = subprocess.Popen(
                cmd, stdout=subprocess.PIPE, stderr=subprocess.PIPE, universal_newlines=True,
                encoding='utf-8', errors='ignore', bufsize=1,
                creationflags=getattr(subprocess, 'CREATE_NO_WINDOW', 0)
            )
        except FileNotFoundError:
            self.update_log(f"ОШИБКА: Не найден Python: {self.script_path}"); self.update_status("Ошибка: Python не найден"); return False
        except Exception as e:
            self.update_log(f"ОШИБКА при запуске subprocess: {e}"); self.update_status("Ошибка запуска процесса"); return False

        stdout_queue = queue.Queue(); stderr_queue = queue.Queue()
        def read_output(pipe, queue_obj):
            try:
                for line in iter(pipe.readline, ''):
                    if line: queue_obj.put(line.strip())
            finally:
                try: pipe.close()
                except OSError: pass
        stdout_thread = threading.Thread(target=read_output, args=(process.stdout, stdout_queue), daemon=True); stderr_thread = threading.Thread(target=read_output, args=(process.stderr, stderr_queue), daemon=True)
        stdout_thread.start(); stderr_thread.start()

        def process_queues():
            msgs_processed = 0
            while not stdout_queue.empty():
                try:
                    line = stdout_queue.get_nowait()
                    if not line.startswith("Found existing installation:") and not line.startswith("Uninstalling"): self.update_log(line)
                    else: self.update_log(f"PIP_STDOUT: {line}")
                    msgs_processed += 1
                except queue.Empty: break
            while not stderr_queue.empty():
                try:
                    line = stderr_queue.get_nowait()
                    if "WARNING:" in line.upper() or "DEPRECATION:" in line.upper(): self.update_log(f"INFO: {line}")
                    elif "ERROR:" in line.upper(): self.update_log(f"ОШИБКА: {line}")
                    elif line.strip(): self.update_log(f"STDERR: {line}")
                    msgs_processed += 1
                except queue.Empty: break
            return msgs_processed

        start_time = time.time(); last_activity_time = start_time
        timeout = 7200; no_activity_timeout = 1800
        while process.poll() is None:
            if self.progress_window and not self.progress_window.winfo_exists():
                self.update_log("Окно прогресса закрыто, прерываем процесс."); process.terminate(); time.sleep(0.5); process.kill(); return False
            msgs_processed = process_queues()
            if msgs_processed > 0: last_activity_time = time.time()
            if time.time() - last_activity_time > no_activity_timeout:
                self.update_log(f"Предупреждение: Процесс неактивен > {no_activity_timeout // 60} мин. Завершение."); process.terminate(); time.sleep(1); process.kill(); return False
            if time.time() - start_time > timeout:
                self.update_log(f"Превышено время выполнения (> {timeout // 60} мин). Завершение."); process.terminate(); time.sleep(1); process.kill(); return False
            if self.progress_window and self.progress_window.winfo_exists():
                try: self.progress_window.update()
                except tk.TclError: self.update_log("Окно прогресса закрыто."); process.terminate(); time.sleep(0.5); process.kill(); return False
            time.sleep(0.1)
        stdout_thread.join(timeout=5); stderr_thread.join(timeout=5)
        process_queues()
        returncode = process.returncode
        self.update_log(f"Процесс pip завершился с кодом: {returncode} ({'Успех' if returncode == 0 else 'Ошибка'})")
        return returncode == 0