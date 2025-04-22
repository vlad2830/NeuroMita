# LocalVoice.py
# Файл для установки локальной озвучки. 
# Из того, что можно сделать - вынести ffmpeg функцию в AudioConverter.py

import importlib
import queue
import threading

import subprocess
import sys
import os
import asyncio
import pygame
import tkinter as tk
from tkinter import ttk
import tkinter.font as tkFont
import time
import ffmpeg

import hashlib
from datetime import datetime

import traceback
import site
import tempfile
import gc
import soundfile as sf

import re
from xml.sax.saxutils import escape
from utils.PipInstaller import PipInstaller

import json
from docs import DocsManager

last_used_tts_rvc = None
tts_rvc = None
fish_speech_tts = None
last_compile = None



class LocalVoice:
    def __init__(self, parent=None):
        self.first_compiled = None
        self.parent = parent
        self.current_model = None
        self.pth_path = None
        self.index_path = None
        self.clone_voice_folder = "Models"
        self.clone_voice_filename = None
        self.clone_voice_text = None

        self.current_tts_rvc = None
        self.current_fish_speech = None
        self.current_silero_model = None # Добавлено
        self.current_silero_sample_rate = 48000 # Добавлено (значение по умолчанию)
        self.silero_models_dir = "checkpoints/silero" # Добавлено

        self.initialized_models = set()
        self.docs_manager = DocsManager()

        self.tts_rvc_module = None
        self.fish_speech_module = None
        self.zonos_module = None
        
        self.amd_test = os.environ.get('TEST_AS_AMD', '').upper() == 'TRUE'

        self.provider = self.check_gpu_provider()

        if self.provider in ["AMD"] or self.amd_test:
            print("KMP_DUPLICATE_LIB_OK = TRUE")
            os.environ["KMP_DUPLICATE_LIB_OK"] = "TRUE"

        self.cuda_found = False
        self.winsdk_found = False
        self.msvc_found = False
        self.triton_installed = False
        self.triton_checks_performed = False
        self._dialog_choice = None

        try:
            from tts_with_rvc import TTS_RVC
            self.tts_rvc_module = TTS_RVC
        except ImportError:
            print("Модуль tts_with_rvc не установлен")


        try:
            from fish_speech_lib.inference import FishSpeech
            self.fish_speech_module = FishSpeech
        except ImportError:
            print("Модуль fish_speech_lib не установлен")


        try:
            import zonos123
            self.zonos_module = zonos123
        except ImportError:
            print("Модуль zonos не установлен")

        if getattr(sys, 'frozen', False):
            base_dir = os.path.dirname(sys.executable)
            alt_base_dir = sys._MEIPASS
        else:
            base_dir = os.path.dirname(__file__)
            alt_base_dir = base_dir

        self._check_system_dependencies()

    def download_model(self, model_id):
        """Загрузка модели по её идентификатору"""
        if model_id == "low":
            try:
                if not self.is_edge_tts_rvc_installed():
                    if not self.download_edge_tts_rvc():
                        return False

                self.current_model = "low"
                return True
            except Exception as ex:
                print("error:", ex)
                return False
        elif model_id == "low+": # Добавлен блок
            try:
                if not self.is_edge_tts_rvc_installed():
                    if not self.download_edge_tts_rvc():
                        return False
                # Загрузка самой модели Silero происходит при инициализации
                self.current_model = "low+"
                return True
            except Exception as ex:
                print("error:", ex)
                return False

        elif model_id == "medium":
            try:
                if not self.is_fish_speech_installed():
                    if not self.download_fish_speech():
                        return False
                self.current_model = "medium"
                return True
            except Exception as ex:
                print("error:", ex)
                return False
        elif model_id == "medium+":
            try:
                if not self.is_fish_speech_installed():
                    if not self.download_fish_speech():
                        return False
                if not self.is_triton_installed():
                    if not self.download_triton():
                        return False
                self.current_model = "medium+"
                return True
            except Exception as ex:
                print("error:", ex)
                return False

        elif model_id == "medium+low":
            try:
                if not self.is_fish_speech_installed():
                    if not self.download_fish_speech():
                        return False
                if not self.is_edge_tts_rvc_installed():
                    if not self.download_edge_tts_rvc():
                        return False
                self.current_model = "medium+low"
                return True
            except Exception as ex:
                print("error:", ex)
                return False
        # elif model_id == "high":
        #     return self.download_zonos()
        else:
            try:
                raise ValueError(f"Неизвестный идентификатор модели: {model_id}")
            except Exception as ex:
                print("error:", ex)
                return False
            
    def _check_system_dependencies(self):
        """Проверяет наличие CUDA, Windows SDK и MSVC с помощью triton (исправлено)."""
        self.cuda_found = False
        self.winsdk_found = False
        self.msvc_found = False
        self.triton_installed = False
        self.triton_checks_performed = False


        libs_path_abs = os.path.abspath("Lib")
        if libs_path_abs not in sys.path:
            sys.path.insert(0, libs_path_abs)
            print(f"Добавлен путь {libs_path_abs} в sys.path для поиска Triton")

        try:
            import triton
            from triton.windows_utils import find_cuda, find_winsdk, find_msvc
            self.triton_installed = True
            print("Triton импортирован успешно.")

            try:
                cuda_result = find_cuda()
                print(f"CUDA find_cuda() result: {cuda_result}")
                if isinstance(cuda_result, (tuple, list)) and len(cuda_result) >= 1:
                    cuda_path = cuda_result[0]
                    self.cuda_found = cuda_path is not None and os.path.exists(str(cuda_path))
                else:
                    self.cuda_found = False
            except Exception as e_cuda:
                 print(f"Ошибка при проверке CUDA: {e_cuda}")
                 self.cuda_found = False
            print(f"CUDA Check: Found={self.cuda_found}")

            try:
                winsdk_result = find_winsdk(False)
                print(f"WinSDK find_winsdk() result: {winsdk_result}")
                if isinstance(winsdk_result, (tuple, list)) and len(winsdk_result) >= 1:
                    winsdk_paths = winsdk_result[0]
                    self.winsdk_found = isinstance(winsdk_paths, list) and bool(winsdk_paths)
                else:
                    self.winsdk_found = False
            except Exception as e_winsdk:
                 print(f"Ошибка при проверке WinSDK: {e_winsdk}")
                 self.winsdk_found = False
            print(f"WinSDK Check: Found={self.winsdk_found}")

            try:
                msvc_result = find_msvc(False)
                print(f"MSVC find_msvc() result: {msvc_result}")
                if isinstance(msvc_result, (tuple, list)) and len(msvc_result) >= 1:
                    msvc_paths = msvc_result[0]
                    self.msvc_found = isinstance(msvc_paths, list) and bool(msvc_paths)
                else:
                    self.msvc_found = False
            except Exception as e_msvc:
                 print(f"Ошибка при проверке MSVC: {e_msvc}")
                 self.msvc_found = False
            print(f"MSVC Check: Found={self.msvc_found}")

            self.triton_checks_performed = True

        except ImportError:
            print("Triton не установлен или не найден в sys.path. Невозможно проверить зависимости CUDA/WinSDK/MSVC.")
            self.triton_installed = False
        except Exception as e:
            print(f"Общая ошибка при проверке зависимостей Triton: {e}")
            traceback.print_exc()
            self.triton_installed = self.is_triton_installed()
            self.triton_checks_performed = False
    
    def _create_installation_window(self, title, initial_status="Подготовка..."):
        """Создает и настраивает окно прогресса установки."""
        progress_window = None
        try:
            # --- Настройка окна прогресса ---
            if not hasattr(self, '_installation_fonts_created'):
                try:
                    title_font_name = "LocalVoiceInstallTitle"
                    status_font_name = "LocalVoiceInstallStatus"
                    log_font_name = "LocalVoiceInstallLog"

                    try:
                        self._title_font = tkFont.Font(name=title_font_name)
                        # Если шрифт существует, просто обновим его параметры (если нужно)
                        self._title_font.config(family="Segoe UI", size=12, weight="bold")
                        print(f"Используется существующий шрифт: {title_font_name}")
                    except tk.TclError:
                        # Если не существует, создаем
                        self._title_font = tkFont.Font(name=title_font_name, family="Segoe UI", size=12, weight="bold")
                        print(f"Создан новый шрифт: {title_font_name}")

                    try:
                        self._status_font_prog = tkFont.Font(name=status_font_name)
                        self._status_font_prog.config(family="Segoe UI", size=9)
                        print(f"Используется существующий шрифт: {status_font_name}")
                    except tk.TclError:
                        self._status_font_prog = tkFont.Font(name=status_font_name, family="Segoe UI", size=9)
                        print(f"Создан новый шрифт: {status_font_name}")

                    try:
                        self._log_font = tkFont.Font(name=log_font_name)
                        self._log_font.config(family="Consolas", size=9)
                        print(f"Используется существующий шрифт: {log_font_name}")
                    except tk.TclError:
                        self._log_font = tkFont.Font(name=log_font_name, family="Consolas", size=9)
                        print(f"Создан новый шрифт: {log_font_name}")

                    self._installation_fonts_created = True

                except tk.TclError as e:
                    print(f"Критическая ошибка при создании/получении шрифтов: {e}")
                    return None

            
            title_font = self._title_font
            status_font_prog = self._status_font_prog
            log_font = self._log_font

            bg_color = "#1e1e1e"
            fg_color = "#ffffff"
            log_bg_color = "#101010"
            log_fg_color = "#cccccc"
            progress_bar_bg = "#333333" # Фон полосы прогресса (не используется напрямую для canvas)
            progress_bar_trough = "#555555" # Цвет желоба
            progress_bar_color = "#4CAF50" # Цвет самой полосы
            button_bg = "#333333" # Для возможных будущих кнопок

            progress_window = tk.Toplevel(self.parent.root if self.parent and hasattr(self.parent, 'root') else None)
            progress_window.title(title)
            progress_window.geometry("700x400")
            progress_window.configure(bg=bg_color)
            progress_window.resizable(False, False)
            progress_window.attributes('-topmost', True)

            
            tk.Label(progress_window, text=title, font=title_font, bg=bg_color, fg=fg_color).pack(pady=10)

            # Фрейм для статуса и процента
            info_frame = tk.Frame(progress_window, bg=bg_color)
            info_frame.pack(fill=tk.X, padx=10)
            status_label = tk.Label(info_frame, text=initial_status, anchor="w", font=status_font_prog, bg=bg_color, fg=fg_color)
            status_label.pack(side=tk.LEFT, pady=5, fill=tk.X, expand=True)
            progress_value_label = tk.Label(info_frame, text="0%", font=status_font_prog, bg=bg_color, fg=fg_color)
            progress_value_label.pack(side=tk.RIGHT, pady=5)

            # --- Ручной Progress Bar ---
            progress_bar_height = 10
            progress_bar_frame = tk.Frame(progress_window, bg=progress_bar_trough, height=progress_bar_height, relief=tk.FLAT, borderwidth=0)
            progress_bar_frame.pack(pady=5, padx=10, fill=tk.X)
            progress_bar_canvas = tk.Canvas(progress_bar_frame, bg=progress_bar_trough, height=progress_bar_height, highlightthickness=0)
            progress_bar_canvas.pack(fill=tk.BOTH, expand=True)
            # Прямоугольник, который будет менять ширину
            progress_rectangle = progress_bar_canvas.create_rectangle(0, 0, 0, progress_bar_height, fill=progress_bar_color, outline="")

            def update_progress_bar(value):
                # Нормализуем значение от 0 до 100
                value = max(0, min(100, int(value)))
                if progress_window and progress_window.winfo_exists():
                    max_width = progress_bar_canvas.winfo_width() # Получаем текущую ширину canvas
                    if max_width <= 1: # Если ширина еще не определена, ждем
                        progress_window.after(50, lambda: update_progress_bar(value))
                        return
                    fill_width = (value / 100) * max_width
                    progress_bar_canvas.coords(progress_rectangle, 0, 0, fill_width, progress_bar_height)
                    progress_value_label.config(text=f"{value}%") # Обновляем текстовый процент
                    progress_window.update_idletasks() # Обновляем отображение

            # --- Лог ---
            log_frame = tk.Frame(progress_window, bg=bg_color)
            log_frame.pack(fill=tk.BOTH, expand=True, padx=10, pady=10)
            log_text = tk.Text(log_frame, height=15, bg=log_bg_color, fg=log_fg_color, wrap=tk.WORD, font=log_font, relief=tk.FLAT, borderwidth=1, highlightthickness=0, insertbackground=fg_color)
            log_text.pack(side=tk.LEFT, fill=tk.BOTH, expand=True)
            # Используем tk.Scrollbar
            scrollbar = tk.Scrollbar(log_frame, command=log_text.yview, relief=tk.FLAT, troughcolor=bg_color, bg=button_bg, activebackground=progress_bar_trough, elementborderwidth=0, borderwidth=0)
            scrollbar.pack(side=tk.RIGHT, fill=tk.Y)
            log_text.config(yscrollcommand=scrollbar.set)
            log_text.config(state=tk.DISABLED) # Блокируем редактирование

            # Центрирование окна
            progress_window.update_idletasks()
            parent_win = self.parent.root if self.parent and hasattr(self.parent, 'root') else None
            if parent_win and parent_win.winfo_exists():
                x = parent_win.winfo_x() + (parent_win.winfo_width() // 2) - (progress_window.winfo_width() // 2)
                y = parent_win.winfo_y() + (parent_win.winfo_height() // 2) - (progress_window.winfo_height() // 2)
                progress_window.geometry(f"+{x}+{y}")
            else:
                 screen_width = progress_window.winfo_screenwidth(); screen_height = progress_window.winfo_screenheight()
                 x = (screen_width // 2) - (progress_window.winfo_width() // 2); y = (screen_height // 2) - (progress_window.winfo_height() // 2)
                 progress_window.geometry(f'+{x}+{y}')

            progress_window.grab_set() # Модальное окно

            # --- Вспомогательные функции для обновления GUI (возвращаемые) ---
            def update_status(message):
                 if progress_window and progress_window.winfo_exists():
                    status_label.config(text=message)
                    progress_window.update()

            def update_log(text):
                 if progress_window and progress_window.winfo_exists():
                    log_text.config(state=tk.NORMAL)
                    log_text.insert(tk.END, text + "\n")
                    log_text.see(tk.END) # Автопрокрутка
                    log_text.config(state=tk.DISABLED)
                    progress_window.update()

            # Возвращаем словарь с элементами управления
            return {
                "window": progress_window,
                "update_progress": update_progress_bar,
                "update_status": update_status,
                "update_log": update_log
            }

        except Exception as e:
            print(f"Ошибка при создании окна установки: {e}")
            traceback.print_exc()
            if progress_window and progress_window.winfo_exists():
                try:
                    progress_window.destroy()
                except:
                    pass
            return None

    def _show_triton_init_warning_dialog(self):
        """Отображает диалоговое окно с предупреждением о зависимостях Triton."""
        self._dialog_choice = None # Сбрасываем выбор

        # Цвета и шрифты
        bg_color = "#1e1e1e"
        fg_color = "#ffffff"
        button_bg = "#333333"
        button_fg = "#ffffff"
        button_active_bg = "#555555"
        status_found_color = "#4CAF50"
        status_notfound_color = "#F44336"
        orange_color = "orange"

        try:
            # Уникальные имена для шрифтов этого диалога
            dlg_main_font_name = "TritonDialogMainFont"
            dlg_bold_font_name = "TritonDialogBoldFont"
            dlg_status_font_name = "TritonDialogStatusFont"
            dlg_button_font_name = "TritonDialogButtonFont"

            # Пытаемся получить или создать каждый шрифт
            try:
                main_font = tkFont.Font(name=dlg_main_font_name)
                main_font.config(family="Segoe UI", size=10)
            except tk.TclError:
                main_font = tkFont.Font(name=dlg_main_font_name, family="Segoe UI", size=10)

            try:
                bold_font = tkFont.Font(name=dlg_bold_font_name)
                bold_font.config(family="Segoe UI", size=10, weight="bold")
            except tk.TclError:
                bold_font = tkFont.Font(name=dlg_bold_font_name, family="Segoe UI", size=10, weight="bold")

            try:
                status_font = tkFont.Font(name=dlg_status_font_name)
                status_font.config(family="Segoe UI", size=9)
            except tk.TclError:
                status_font = tkFont.Font(name=dlg_status_font_name, family="Segoe UI", size=9)

            try:
                button_font = tkFont.Font(name=dlg_button_font_name)
                button_font.config(family="Segoe UI", size=9, weight="bold")
            except tk.TclError:
                button_font = tkFont.Font(name=dlg_button_font_name, family="Segoe UI", size=9, weight="bold")

        except tk.TclError as e:
            print(f"Критическая ошибка при создании/получении шрифтов для диалога: {e}")
            main_font, bold_font, status_font, button_font = None, None, None, None 

        # Создание окна
        dialog = tk.Toplevel(self.parent.root if self.parent and hasattr(self.parent, 'root') else None)
        dialog.title("⚠️ Зависимости Triton")
        dialog.configure(bg=bg_color)
        dialog.resizable(False, False)
        dialog.attributes('-topmost', True)

        # --- Верхняя часть: Статус ---
        top_frame = tk.Frame(dialog, bg=bg_color, padx=15, pady=10)
        top_frame.pack(fill=tk.X)

        tk.Label(top_frame, text="Статус зависимостей Triton:", font=bold_font, bg=bg_color, fg=fg_color).pack(anchor='w', pady=(0, 5))

        status_frame = tk.Frame(top_frame, bg=bg_color)
        status_frame.pack(fill=tk.X, pady=(0, 10))

        # Словарь для хранения ссылок на метки статуса (для обновления)
        status_label_widgets = {}

        def update_status_display():
            # Очищаем предыдущие метки статуса
            for widget in status_frame.winfo_children():
                widget.destroy()
            status_label_widgets.clear()

            items = [
                ("CUDA Toolkit:", self.cuda_found),
                ("Windows SDK:", self.winsdk_found),
                ("MSVC:", self.msvc_found)
            ]

            for text, found in items:
                item_frame = tk.Frame(status_frame, bg=bg_color)
                # Размещаем элементы горизонтально
                item_frame.pack(side=tk.LEFT, padx=(0, 15), anchor='w')

                label = tk.Label(item_frame, text=text, font=status_font, bg=bg_color, fg=fg_color)
                label.pack(side=tk.LEFT)
                status_text = "Найден" if found else "Не найден"
                status_color = status_found_color if found else status_notfound_color
                status_label_widget = tk.Label(item_frame, text=status_text, font=status_font, bg=bg_color, fg=status_color)
                status_label_widget.pack(side=tk.LEFT, padx=(3, 0))
                # Сохраняем ссылку на метку статуса
                status_label_widgets[text] = status_label_widget

            # Показываем или скрываем предупреждение
            all_found = self.cuda_found and self.winsdk_found and self.msvc_found
            if not all_found:
                if not hasattr(dialog, 'warning_label') or not dialog.warning_label.winfo_exists():
                    warning_text = "⚠️ Модели medium+ и medium+low требуют всех компонентов!"
                    dialog.warning_label = tk.Label(top_frame, text=warning_text, bg=bg_color, fg=orange_color, font=bold_font)
                    # Пакуем под status_frame
                    dialog.warning_label.pack(anchor='w', pady=(5, 0), before=status_frame) # before может не сработать, пакуем после
                    dialog.warning_label.pack_forget() # Сначала убираем
                    dialog.warning_label.pack(anchor='w', pady=(5,0), fill=tk.X) # Пакуем снова внизу top_frame
                dialog.warning_label.config(text="⚠️ Модели medium+ и medium+low требуют всех компонентов!")
                if not dialog.warning_label.winfo_ismapped():
                     dialog.warning_label.pack(anchor='w', pady=(5,0), fill=tk.X)
            elif hasattr(dialog, 'warning_label') and dialog.warning_label.winfo_ismapped():
                dialog.warning_label.pack_forget() # Скрываем, если все найдено

            dialog.update_idletasks() # Обновляем геометрию окна

        update_status_display() # Первоначальное отображение статуса

        # --- Средняя часть: Информация ---
        info_frame = tk.Frame(dialog, bg=bg_color, padx=15, pady=5)
        info_frame.pack(fill=tk.X)
        info_text = (
            "Если компоненты не найдены, установите их согласно документации.\n"
            "Вы также можете попробовать инициализировать модель вручную,\n"
            "запустив `init_triton.bat` в корневой папке программы."
        )
        tk.Label(info_frame, text=info_text, font=main_font, bg=bg_color, fg=fg_color, justify=tk.LEFT).pack(anchor='w')

        # --- Нижняя часть: Кнопки ---
        button_frame = tk.Frame(dialog, bg=bg_color, padx=15, pady=15)
        button_frame.pack(fill=tk.X)

        # Функции для кнопок
        def on_refresh():
            print("Обновление статуса зависимостей...")
            # Блокируем кнопки на время проверки
            refresh_button.config(state=tk.DISABLED, text="Проверка...")
            dialog.update()
            self._check_system_dependencies()
            update_status_display() # Обновляем отображение в диалоге
            refresh_button.config(state=tk.NORMAL, text="Обновить статус")
            print("Статус обновлен.")

        def on_docs():
            self.docs_manager.open_doc("installation_guide.html") 
                
        def on_skip():
            self._dialog_choice = "skip"
            dialog.destroy()

        def on_continue():
            self._dialog_choice = "continue"
            dialog.destroy()

        # Создание кнопок
        continue_button = tk.Button(button_frame, text="Продолжить инициализацию", command=on_continue,
                                    font=button_font, bg=status_found_color, fg=button_fg, relief=tk.FLAT, borderwidth=0,
                                    activebackground=button_active_bg, activeforeground=button_fg, padx=10, pady=3, cursor="hand2")
        continue_button.pack(side=tk.RIGHT, padx=(5, 0))

        skip_button = tk.Button(button_frame, text="Пропустить инициализацию", command=on_skip,
                                font=button_font, bg=button_bg, fg=button_fg, relief=tk.FLAT, borderwidth=0,
                                activebackground=button_active_bg, activeforeground=button_fg, padx=10, pady=3, cursor="hand2")
        skip_button.pack(side=tk.RIGHT, padx=(5, 0))

        docs_button = tk.Button(button_frame, text="Открыть документацию", command=on_docs,
                                font=button_font, bg=button_bg, fg=button_fg, relief=tk.FLAT, borderwidth=0,
                                activebackground=button_active_bg, activeforeground=button_fg, padx=10, pady=3, cursor="hand2")
        docs_button.pack(side=tk.LEFT, padx=(0, 5))

        refresh_button = tk.Button(button_frame, text="Обновить статус", command=on_refresh,
                                   font=button_font, bg=button_bg, fg=button_fg, relief=tk.FLAT, borderwidth=0,
                                   activebackground=button_active_bg, activeforeground=button_fg, padx=10, pady=3, cursor="hand2")
        refresh_button.pack(side=tk.LEFT, padx=(0, 5))


        # Центрирование окна
        dialog.update_idletasks() # Убедимся, что размеры окна рассчитаны
        parent_win = self.parent.root if self.parent and hasattr(self.parent, 'root') else None
        if parent_win and parent_win.winfo_exists():
            # Центрируем относительно родительского окна
            parent_x = parent_win.winfo_x()
            parent_y = parent_win.winfo_y()
            parent_width = parent_win.winfo_width()
            parent_height = parent_win.winfo_height()
            dialog_width = dialog.winfo_width()
            dialog_height = dialog.winfo_height()
            x = parent_x + (parent_width // 2) - (dialog_width // 2)
            y = parent_y + (parent_height // 2) - (dialog_height // 2)
            dialog.geometry(f"+{x}+{y}")
        else:
            # Центрируем относительно экрана
             screen_width = dialog.winfo_screenwidth()
             screen_height = dialog.winfo_screenheight()
             dialog_width = dialog.winfo_width()
             dialog_height = dialog.winfo_height()
             x = (screen_width // 2) - (dialog_width // 2)
             y = (screen_height // 2) - (dialog_height // 2)
             dialog.geometry(f'+{x}+{y}')


        # Делаем окно модальным
        dialog.grab_set() # Перехватываем ввод
        dialog.wait_window() # Ждем закрытия окна

        return self._dialog_choice # Возвращаем выбор пользователя

    #region Установка моделей

    def download_triton(self):
        gui_elements = None
        try:
            gui_elements = self._create_installation_window(
                title="Установка Triton",
                initial_status="Подготовка..."
            )
            if not gui_elements:
                return False # Ошибка создания окна

            progress_window = gui_elements["window"]
            update_progress = gui_elements["update_progress"]
            update_status = gui_elements["update_status"]
            update_log = gui_elements["update_log"]

            # --- Логика установки ---
            script_path = r"libs\python\python.exe"
            libs_path = "Lib"
            libs_path_abs = os.path.abspath(libs_path)

            if not os.path.exists(libs_path):
                os.makedirs(libs_path)
                update_log(f"Создана директория: {libs_path}")

            if libs_path_abs not in sys.path:
                 sys.path.insert(0, libs_path_abs)
                 update_log(f"Добавлен путь {libs_path_abs} в sys.path")


            update_progress(10)
            update_log("Начало установки Triton...")


            # --- Шаги установки ---
            update_progress(20)
            update_status("Установка библиотеки Triton...")
            update_log("Установка библиотеки triton-windows...")

            script_path = r"libs\python\python.exe" # Путь к Python остается здесь или передается в LocalVoice.__init__
            installer = PipInstaller(
                script_path=script_path,
                libs_path="Lib",
                update_status=update_status,
                update_log=update_log,
                progress_window=progress_window # Передаем окно для проверки
            )


            success = installer.install_package(
                "triton-windows<3.3",
                description="Установка библиотеки Triton...",
                extra_args=["--upgrade"]
            )
            
            update_progress(10)
            update_log("Начало установки Triton...")

            if not success:
                update_status("Ошибка при установке Triton")
                update_log("Не удалось установить Triton. Проверьте лог выше.")
                if progress_window and progress_window.winfo_exists():
                    progress_window.after(5000, progress_window.destroy)
                return False

            # --- Патчи ---
            update_progress(50)
            update_status("Применение патчей...")
            update_log("Применение патча к build.py...")
            build_py_path = os.path.join(libs_path_abs, "triton", "runtime", "build.py")
            if os.path.exists(build_py_path):
                try:
                    with open(build_py_path, "r", encoding="utf-8") as f: source = f.read()
                    new_cc_path = os.path.join(libs_path_abs, "triton", "runtime", "tcc", "tcc.exe").replace("\\", "\\\\")
                    old_line = 'cc = os.path.join(sysconfig.get_paths()["platlib"], "triton", "runtime", "tcc", "tcc.exe")'
                    new_line = f'cc = r"{new_cc_path}"'
                    if old_line in source:
                        patched_source = source.replace(old_line, new_line)
                        with open(build_py_path, "w", encoding="utf-8") as f: f.write(patched_source)
                        update_log("Патч успешно применен к build.py")
                    else:
                        update_log("Патч для build.py уже применен или строка не найдена.")
                except Exception as e: update_log(f"Ошибка при патче build.py: {e}")
            else: update_log("Предупреждение: файл build.py не найден, пропускаем патч")

            update_progress(60)
            update_log("Применение патча к windows_utils.py...")
            windows_utils_path = os.path.join(libs_path_abs, "triton", "windows_utils.py")
            if os.path.exists(windows_utils_path):
                try:
                    with open(windows_utils_path, "r", encoding="utf-8") as f: source = f.read()
                    old_code_win = "output = subprocess.check_output(command, text=True).strip()"
                    new_code_win = "output = subprocess.check_output(\n            command, text=True, creationflags=subprocess.CREATE_NO_WINDOW, close_fds=True, stdin=subprocess.DEVNULL, stderr=subprocess.PIPE\n        ).strip()"
                    if old_code_win in source:
                        patched_source = source.replace(old_code_win, new_code_win)
                        with open(windows_utils_path, "w", encoding="utf-8") as f: f.write(patched_source)
                        update_log("Патч успешно применен к windows_utils.py")
                    else:
                         update_log("Патч для windows_utils.py уже применен или строка не найдена.")
                except Exception as e: update_log(f"Ошибка при патче windows_utils.py: {e}")
            else: update_log("Предупреждение: файл windows_utils.py не найден, пропускаем патч")

            update_progress(70)
            update_log("Применение патча к compiler.py...")
            compiler_path = os.path.join(libs_path_abs, "triton", "backends", "nvidia", "compiler.py")
            if os.path.exists(compiler_path):
                try:
                    with open(compiler_path, "r", encoding="utf-8") as f: source = f.read()
                    old_code_comp_line = 'version = subprocess.check_output([_path_to_binary("ptxas")[0], "--version"]).decode("utf-8")'
                    new_code_comp_line = 'version = subprocess.check_output([_path_to_binary("ptxas")[0], "--version"], creationflags=subprocess.CREATE_NO_WINDOW, stderr=subprocess.PIPE, close_fds=True, stdin=subprocess.DEVNULL).decode("utf-8")'
                    if old_code_comp_line in source:
                        patched_source = source.replace(old_code_comp_line, new_code_comp_line)
                        with open(compiler_path, "w", encoding="utf-8") as f: f.write(patched_source)
                        update_log("Патч успешно применен к compiler.py")
                    else:
                        update_log("Патч для compiler.py уже применен или строка не найдена.")
                except Exception as e: update_log(f"Ошибка при патче compiler.py: {e}")
            else: update_log("Предупреждение: файл compiler.py не найден, пропускаем патч")

            # --- Проверка зависимостей и диалог ---
            update_progress(80)
            update_status("Проверка зависимостей...")
            update_log("Проверка наличия CUDA, Windows SDK, MSVC...")
            self._check_system_dependencies()

            skip_init = False
            user_action = None

            if self.triton_installed and self.triton_checks_performed:
                if not (self.cuda_found and self.winsdk_found and self.msvc_found):
                    update_log("Обнаружено отсутствие некоторых зависимостей Triton.")
                    update_status("Требуется внимание: зависимости Triton")
                    if progress_window and progress_window.winfo_exists():
                        progress_window.grab_release()
                        progress_window.attributes('-topmost', False)

                    user_action = self._show_triton_init_warning_dialog()

                    if progress_window and progress_window.winfo_exists():
                        progress_window.attributes('-topmost', True)
                        progress_window.grab_set()

                    if user_action == "skip":
                        update_log("Пользователь выбрал пропустить инициализацию ядра Triton.")
                        skip_init = True
                    elif user_action == "continue":
                        update_log("Пользователь выбрал продолжить инициализацию, несмотря на отсутствующие зависимости.")
                        skip_init = False
                    else:
                        update_log("Инициализация ядра Triton отменена (диалог закрыт).")
                        skip_init = True
                else:
                    update_log("Все зависимости Triton (CUDA, WinSDK, MSVC) найдены.")
                    skip_init = False
            elif not self.triton_installed:
                 update_log("Triton не установлен корректно, инициализация ядра невозможна.")
                 skip_init = True
            else:
                 update_log("Ошибка во время проверки зависимостей Triton. Инициализация ядра будет пропущена.")
                 skip_init = True

            # --- Инициализация ядра (init.py) ---
            if not skip_init:
                update_status("Инициализация ядра Triton...")
                update_log("Начало инициализации ядра (запуск init.py)...")
                try:
                    temp_dir = "temp"
                    if not os.path.exists(temp_dir):
                        os.makedirs(temp_dir)
                        update_log(f"Создана директория: {temp_dir}")

                    update_log("Запуск скрипта инициализации...")

                    init_cmd = [script_path, "init.py"]
                    update_log(f"Выполняем: {' '.join(init_cmd)}")
                    try:
                        # Запускаем и ждем завершения, захватывая вывод
                        result = subprocess.run(
                            init_cmd,
                            capture_output=True, # Захватываем stdout и stderr
                            text=True,           # Декодируем как текст
                            encoding='utf-8',
                            errors='ignore',
                            check=False,         # Не выбрасывать исключение при ненулевом коде возврата
                            creationflags=subprocess.CREATE_NO_WINDOW
                        )
                        # Логируем вывод
                        if result.stdout:
                            update_log("--- Вывод init.py (stdout) ---")
                            for line in result.stdout.splitlines():
                                update_log(line)
                            update_log("--- Конец вывода init.py (stdout) ---")
                        if result.stderr:
                            update_log("--- Вывод init.py (stderr) ---")
                            for line in result.stderr.splitlines():
                                update_log(f"STDERR: {line}") # Помечаем как stderr
                            update_log("--- Конец вывода init.py (stderr) ---")

                        update_log(f"Скрипт init.py завершился с кодом: {result.returncode}")
                        init_success = (result.returncode == 0)

                    except Exception as sub_e:
                        update_log(f"Ошибка при запуске init.py через subprocess.run: {sub_e}")
                        init_success = False

                    if not init_success:
                        update_status("Ошибка при инициализации ядра")
                        update_log("Ошибка при запуске init.py. Проверьте лог выше.")
                    else:
                        output_file_path = os.path.join(temp_dir, "inited.wav")
                        if os.path.exists(output_file_path):
                            update_log(f"Проверка успешна: файл {output_file_path} создан")
                            update_progress(95)
                            update_status("Инициализация ядра успешно завершена!")
                        else:
                            update_log(f"Предупреждение: Файл {output_file_path} не найден после init.py")
                            update_status("Предупреждение: Файл инициализации не создан")
                            update_progress(90)

                except Exception as e:
                    update_log(f"Непредвиденная ошибка при инициализации ядра: {str(e)}")
                    trace = traceback.format_exc()
                    update_log(trace)
                    update_status("Ошибка инициализации ядра")

            else:
                update_log("Инициализация ядра Triton пропущена.")
                update_status("Инициализация ядра пропущена")
                update_progress(95)

            # --- Завершение ---
            update_progress(100)
            final_message = "Установка Triton завершена."
            if skip_init:
                final_message += " Инициализация ядра пропущена."
            if self.triton_installed and self.triton_checks_performed and not (self.cuda_found and self.winsdk_found and self.msvc_found):
                 final_message += " Внимание: не все зависимости найдены!"

            update_status(final_message)
            update_log(final_message)
            update_log("Если модель medium+ не заработает, проверьте зависимости и попробуйте запустить init_triton.bat.")

            self.current_model = "medium+"
            if progress_window and progress_window.winfo_exists():
                progress_window.after(5000, progress_window.destroy)
            return True

        except Exception as e:
            print(f"Критическая ошибка при установке Triton: {e}")
            traceback.print_exc()
            try:
                if gui_elements and gui_elements["window"] and gui_elements["window"].winfo_exists():
                    gui_elements["update_log"](f"КРИТИЧЕСКАЯ ОШИБКА: {e}")
                    gui_elements["update_log"](traceback.format_exc())
                    gui_elements["update_status"]("Критическая ошибка установки!")
                    gui_elements["window"].after(10000, gui_elements["window"].destroy)
            except Exception as e_inner:
                 print(f"Ошибка при попытке обновить лог в окне прогресса: {e_inner}")
            return False
        finally:
             try:
                 if gui_elements and gui_elements["window"] and gui_elements["window"].winfo_exists():
                     # Не закрываем здесь, так как есть after(5000) выше
                     pass
             except Exception as e_final:
                 print(f"Ошибка при закрытии окна прогресса: {e_final}")

    def download_edge_tts_rvc(self):
        """Загружает Edge-TTS + RVC модель"""
        gui_elements = None
        try:
            gui_elements = self._create_installation_window(
                title="Скачивание Edge-TTS + RVC",
                initial_status="Подготовка..."
            )
            if not gui_elements:
                return False

            progress_window = gui_elements["window"]
            update_progress = gui_elements["update_progress"]
            update_status = gui_elements["update_status"]
            update_log = gui_elements["update_log"]

            script_path = r"libs\python\python.exe"
            installer = PipInstaller(
                script_path=script_path,
                libs_path="Lib",
                update_status=update_status,
                update_log=update_log,
                progress_window=progress_window
            )

            update_progress(10)
            update_log("Начало установки Edge-TTS + RVC...")

            # Сначала устанавливаем PyTorch с CUDA 12.4 (если нужно)
            if self.provider in ["NVIDIA"] and not self.is_cuda_available():
                update_status("Установка PyTorch с поддержкой CUDA 12.4...")
                update_progress(20)
                success = installer.install_package(
                    ["torch", "torchaudio"],
                    description="Установка PyTorch с поддержкой CUDA 12.4...",
                    extra_args=["--index-url", "https://download.pytorch.org/whl/cu124"]
                )

                if not success:
                    update_status("Ошибка при установке PyTorch")
                    if progress_window and progress_window.winfo_exists():
                        progress_window.after(5000, progress_window.destroy)
                    return False
                update_progress(50)
            else:
                update_progress(50) # Пропускаем шаг PyTorch, но двигаем прогресс

            update_status("Установка зависимостей...")
            success = installer.install_package(
                "omegaconf",
                description="Установка omegaconf..."
            )
            if not success:
                update_status("Ошибка при установке omegaconf")
                if progress_window and progress_window.winfo_exists():
                    progress_window.after(5000, progress_window.destroy)
                return False

            update_progress(70)

            # Устанавливаем основную библиотеку
            update_status("Установка основной библиотеки...")
            try:
                package_url = None
                desc = ""
                if self.provider in ["NVIDIA"]:
                    package_url = "git+https://github.com/Atm4x/tts-with-rvc.git#egg=tts_with_rvc"
                    desc = "Установка основной библиотеки tts-with-rvc (NVIDIA)..."
                elif self.provider in ["AMD"]:
                    package_url = "git+https://github.com/Atm4x/tts-with-rvc.git@onnx#egg=tts_with_rvc_onnx"
                    desc = "Установка основной библиотеки tts-with-rvc (AMD)..."
                else:
                    update_log(f"Ошибка: не найдена подходящая видеокарта: {self.provider}")
                    if progress_window and progress_window.winfo_exists():
                        progress_window.after(5000, progress_window.destroy)
                    return False

                success = installer.install_package(package_url, description=desc)

                if not success:
                    update_status("Ошибка при установке tts-with-rvc")
                    if progress_window and progress_window.winfo_exists():
                        progress_window.after(5000, progress_window.destroy)
                    return False

            except Exception as e:
                update_log(f"Ошибка при установке tts-with-rvc: {e}")
                if progress_window and progress_window.winfo_exists():
                    progress_window.after(5000, progress_window.destroy)
                return False

            
            libs_path = "Lib"
            libs_path_abs = os.path.abspath(libs_path)
            update_progress(95)
            update_status("Применение патчей...")
            config_path = os.path.join(libs_path_abs, "fairseq", "dataclass", "configs.py")
            if os.path.exists(config_path):
                try:
                    import re
                    with open(config_path, "r", encoding="utf-8") as f:
                        source = f.read()
                    patched_source = re.sub(r"metadata=\{(.*?)help:", r'metadata={\1"help":', source)
                    with open(config_path, "w", encoding="utf-8") as f:
                        f.write(patched_source)
                    update_log("Патч успешно применен к configs.py")
                except Exception as e:
                    update_log(f"Ошибка при патче configs.py: {e}")
            else:
                update_log("Предупреждение: файл configs.py не найден, пропускаем патч")

            update_progress(100)

            update_status("Попытка импорта модуля...")
            update_log("Установка успешно завершена! Попытка импорта модуля...")

            # Пытаемся импортировать модуль
            try:
                import importlib
                import sys

                # Удаляем старые импорты, если они есть
                if "tts_with_rvc" in sys.modules: del sys.modules["tts_with_rvc"]
                if "tts_with_rvc_onnx" in sys.modules: del sys.modules["tts_with_rvc_onnx"]
                # Очищаем кэш импорта для пути Lib
                importlib.invalidate_caches()

                # Добавляем путь снова, если его нет (на всякий случай)
                if libs_path_abs not in sys.path:
                    sys.path.insert(0, libs_path_abs)

                # Импортируем
                from tts_with_rvc import TTS_RVC
                self.tts_rvc_module = TTS_RVC
                update_log("Модуль успешно загружен без перезапуска программы!")
            except ImportError as ie:
                update_log(f"Предупреждение: Модуль установлен, но не может быть загружен немедленно: {ie}")
                update_log("При следующем использовании он будет доступен.")
            except Exception as e_imp:
                 update_log(f"Ошибка при попытке импорта модуля после установки: {e_imp}")
                 traceback.print_exc()


            if progress_window and progress_window.winfo_exists():
                progress_window.after(3000, progress_window.destroy)

            self.current_model = "low"
            return True

        except Exception as e:
            print(f"Ошибка при установке Edge-TTS + RVC: {e}")
            traceback.print_exc()
            if gui_elements and gui_elements["window"] and gui_elements["window"].winfo_exists():
                try:
                    gui_elements["update_log"](f"КРИТИЧЕСКАЯ ОШИБКА: {e}\n{traceback.format_exc()}")
                    gui_elements["update_status"]("Критическая ошибка установки!")
                    gui_elements["window"].after(10000, gui_elements["window"].destroy)
                except: pass
            return False
        finally:
            if gui_elements and gui_elements["window"] and gui_elements["window"].winfo_exists():
                pass

    def download_fish_speech(self):
        """Загружает Fish Speech модель"""
        gui_elements = None
        try:
            gui_elements = self._create_installation_window(
                title="Скачивание Fish Speech",
                initial_status="Подготовка..."
            )
            if not gui_elements:
                return False

            progress_window = gui_elements["window"]
            update_progress = gui_elements["update_progress"]
            update_status = gui_elements["update_status"]
            update_log = gui_elements["update_log"]

            # script_path = r"libs\python\python.exe"
            

            script_path = r"libs\python\python.exe"
            installer = PipInstaller(
                script_path=script_path,
                libs_path="Lib",
                update_status=update_status,
                update_log=update_log,
                progress_window=progress_window
            )

            update_progress(10)
            update_log("Начало установки Fish Speech...")

            # Сначала устанавливаем PyTorch с CUDA 12.4 (если нужно)
            if self.provider in ["NVIDIA"] and not self.is_cuda_available():
                update_status("Установка PyTorch с поддержкой CUDA 12.4...")
                update_progress(20)
                success = installer.install_package(
                    ["torch", "torchaudio"],
                    description="Установка PyTorch с поддержкой CUDA 12.4...",
                    extra_args=["--index-url", "https://download.pytorch.org/whl/cu124"]
                )

                if not success:
                    update_status("Ошибка при установке PyTorch")
                    if progress_window and progress_window.winfo_exists():
                        progress_window.after(5000, progress_window.destroy)
                    return False
                update_progress(40)
            else:
                 update_progress(40) # Пропускаем шаг PyTorch, но двигаем прогресс

            update_status("Установка библиотеки Fish Speech...")

            try:
                force_install_unsupported = os.environ.get("ALLOW_UNSUPPORTED_GPU", "0") == "1"
                if self.provider in ["NVIDIA"] or force_install_unsupported:
                    if force_install_unsupported:
                        update_log("--------------------------------")
                        update_log(f"ВНИМАНИЕ! УСТАНОВКА НА ВИДЕОКАРТУ {self.provider} НЕСОВМЕСТИМОЙ МОДЕЛИ!!")
                        update_log("--------------------------------")

                    success = installer.install_package(
                        "git+https://github.com/Atm4x/Fish-speech-pipeline#egg=fish_speech_lib",
                        description="Установка библиотеки Fish Speech..."
                    )

                    if not success:
                        update_status("Ошибка при установке Fish Speech")
                        if progress_window and progress_window.winfo_exists():
                            progress_window.after(5000, progress_window.destroy)
                        return False

                    update_progress(80)

                    # Дополнительно устанавливаем librosa
                    update_status("Установка дополнительных библиотек...")
                    success = installer.install_package(
                        "librosa==0.9.1",
                        description="Установка дополнительной библиотеки librosa..."
                    )

                    if not success:
                        update_status("Ошибка при установке librosa")
                        update_log("Предупреждение: Fish Speech может работать некорректно без librosa")
                        # Не прерываем установку из-за librosa
                else:
                    update_log(f"Ошибка: не найдена подходящая видеокарта: {self.provider}")
                    update_status("Требуется NVIDIA GPU")
                    if progress_window and progress_window.winfo_exists():
                        progress_window.after(5000, progress_window.destroy)
                    return False

            except Exception as e:
                update_log(f"Ошибка при установке Fish Speech: {e}")
                if progress_window and progress_window.winfo_exists():
                    progress_window.after(5000, progress_window.destroy)
                return False

            update_progress(100)
            
            update_status("Попытка импорта модуля...")
            update_log("Установка успешно завершена! Попытка импорта модуля...")

            # Пытаемся импортировать модуль
            try:
                import importlib
                import sys
                libs_path = "Lib"
                libs_path_abs = os.path.abspath(libs_path)

                if "fish_speech_lib" in sys.modules: del sys.modules["fish_speech_lib"]
                importlib.invalidate_caches()
                if libs_path_abs not in sys.path:
                    sys.path.insert(0, libs_path_abs)

                from fish_speech_lib.inference import FishSpeech
                self.fish_speech_module = FishSpeech
                update_log("Модуль успешно загружен без перезапуска программы!")
            except ImportError as ie:
                update_log(f"Предупреждение: Модуль установлен, но не может быть загружен немедленно: {ie}")
                update_log("При следующем использовании он будет доступен.")
            except Exception as e_imp:
                 update_log(f"Ошибка при попытке импорта модуля после установки: {e_imp}")
                 traceback.print_exc()

            if progress_window and progress_window.winfo_exists():
                progress_window.after(3000, progress_window.destroy)

            self.current_model = "medium"
            return True

        except Exception as e:
            print(f"Ошибка при установке Fish Speech: {e}")
            traceback.print_exc()
            if gui_elements and gui_elements["window"] and gui_elements["window"].winfo_exists():
                try:
                    gui_elements["update_log"](f"КРИТИЧЕСКАЯ ОШИБКА: {e}\n{traceback.format_exc()}")
                    gui_elements["update_status"]("Критическая ошибка установки!")
                    gui_elements["window"].after(10000, gui_elements["window"].destroy)
                except: pass
            return False
        finally:
             if gui_elements and gui_elements["window"] and gui_elements["window"].winfo_exists():
                 # Не закрываем здесь, так как есть after(3000) выше
                 pass
             
    #endregion
    async def voiceover(self, text, output_file="output.wav", character=None):
        """Создает озвучку текста с использованием текущей модели"""
        if self.current_model is None:
            raise ValueError("Модель не выбрана. Сначала скачайте и выберите модель.")

        # Получаем путь к модели персонажа, если есть
        if character is not None:
            is_nvidia = self.provider in ["NVIDIA"]

            short_name = str(getattr(character, 'short_name', None))
            self.pth_path = os.path.join(os.path.abspath(self.clone_voice_folder), f"{short_name}.{'pth' if is_nvidia else 'onnx'}")
            self.index_path = os.path.join(os.path.abspath(self.clone_voice_folder), f"{short_name}.index")
            self.clone_voice_filename = os.path.join(os.path.abspath(self.clone_voice_folder), f"{short_name}.wav")
            self.clone_voice_text = os.path.join(os.path.abspath(self.clone_voice_folder), f"{short_name}.txt")
            print(f"Используем модель персонажа: {self.pth_path if self.pth_path else 'не указана'}")
            print(f"Используем .index файл персонажа: {self.index_path if self.index_path else 'не указан'}")

        # Проверяем, инициализирована ли текущая модель
        if self.current_model == "low" and not self.is_model_initialized("low"):
            if not self.initialize_model("low"):
                raise Exception("Не удалось инициализировать модель low")
        elif self.current_model == "low+" and not self.is_model_initialized("low+"):
            if not self.initialize_model("low+"):
                raise Exception("Не удалось инициализировать модель low+")
        elif self.current_model == "medium" and not self.is_model_initialized("medium"):
            if not self.initialize_model("medium"):
                raise Exception("Не удалось инициализировать модель medium")
        elif self.current_model == "medium+" and not self.is_model_initialized("medium"):
            if not self.initialize_model("medium+"):
                raise Exception("Не удалось инициализировать модель medium+")
        elif self.current_model == "medium+low" and not self.is_model_initialized("medium+low"):
            if not self.initialize_model("medium+low"):
                raise Exception("Не удалось инициализировать модель medium+low")

        # Используем соответствующий метод озвучки
        if self.current_model == "low":
            return await self.voiceover_edge_tts_rvc(text)
        elif self.current_model == "low+": # Добавлено
            return await self.voiceover_silero_rvc(text, character)
        elif self.current_model == "medium":
            return await self.voiceover_fish_speech(text)
        elif self.current_model == "medium+":
            return await self.voiceover_fish_speech(text, compile=True)
        elif self.current_model == "medium+low":
            return await self.voiceover_fish_speech(text, compile=True, with_rvc=True)
        elif self.current_model == "high":
            return await self.voiceover_zonos(text, character)
        else:
            raise ValueError(f"Неизвестная модель: {self.current_model}")
        
    # region На вынос в AudioConverter.py
    async def convert_wav_to_stereo(self, input_path, output_path, atempo: float = 1, volume: str = "1.0"):
        """
        Конвертирует WAV из 40 кГц моно в 44.1 кГц стерео с замедлением для компенсации
        разницы между 40 кГц и 32 кГц при воспроизведении.
        """
        try:
            if not os.path.exists(input_path):
                print(f"Файл {input_path} не найден при попытке конвертации.")
                return None

            print(f"Начинаю конвертацию {input_path} в {output_path} с помощью ffmpeg")

            (
                ffmpeg
                .input(input_path)
                .filter('atempo', atempo) # Замедляем аудио на x%
                .filter('volume', volume=volume)  
                .output(
                    output_path,
                    format="wav",      # Указываем формат WAV
                    acodec="pcm_s16le", # 16-битный PCM
                    ar="44100",        # Частота дискретизации 44100 Hz
                    ac=2               # Количество каналов (2 = стерео)
                )
                .run(cmd=["ffmpeg", "-nostdin"], capture_stdout=True, capture_stderr=True)
            )

            print(f"Конвертация завершена: {output_path}")
            return output_path
        except Exception as e:
            print(f"Ошибка при конвертации WAV в стерео: {e}")
            return None
    # endregion
    async def voiceover_edge_tts_rvc(self, text, TEST_WITH_DONE_AUDIO: str = None):
        if self.tts_rvc_module is None:
            raise ImportError("Модуль tts_with_rvc не установлен. Сначала установите модель 'low' или 'medium+low'.")
        try:
            if not self.current_tts_rvc:
                model_to_init = None
                if self.current_model == "low":
                    model_to_init = "low"
                elif self.current_model == "medium+low":
                    model_to_init = "medium+low"
                if model_to_init:
                    print(f"Предупреждение: Экземпляр TTS_RVC не инициализирован для модели {self.current_model}. Попытка инициализации...")
                    if not self.initialize_model(model_to_init):
                        raise Exception(f"Не удалось инициализировать компонент TTS_RVC для модели {self.current_model}")
                else:
                    raise Exception(f"Неожиданный вызов voiceover_edge_tts_rvc для модели {self.current_model}, которая не использует RVC.")
            settings = self.load_model_settings(self.current_model)
            if not settings:
                print(f"Предупреждение: Не найдены настройки для модели {self.current_model}. Используются значения по умолчанию.")

            is_combined_model = self.current_model == "medium+low"

            pitch_key = "fsprvc_rvc_pitch" if is_combined_model else "pitch"
            index_rate_key = "fsprvc_index_rate" if is_combined_model else "index_rate"
            protect_key = "fsprvc_protect" if is_combined_model else "protect"
            tts_rate_key = "tts_rate"
            filter_radius_key = "fsprvc_filter_radius" if is_combined_model else "filter_radius"
            rms_mix_rate_key = "fsprvc_rvc_rms_mix_rate" if is_combined_model else "rms_mix_rate"
            is_half_key = "fsprvc_is_half" if is_combined_model else "is_half"
            f0method_key = "fsprvc_f0method" if is_combined_model else "f0method"
            use_index_file_key = "fsprvc_use_index_file" if is_combined_model else "use_index_file"

            pitch = float(settings.get(pitch_key, 0))
            index_rate = float(settings.get(index_rate_key, 0.75))
            protect = float(settings.get(protect_key, 0.33))
            filter_radius = int(settings.get(filter_radius_key, 3))
            rms_mix_rate = float(settings.get(rms_mix_rate_key, 0.5))
            is_half_str = settings.get(is_half_key, "True")
            is_half = is_half_str.lower() == "true"
            use_index_file = settings.get(use_index_file_key, True)

            if use_index_file:
                self.current_tts_rvc.set_index_path(self.index_path)
                print("Используем индексную базу для RVC:", self.index_path)
            else:
                self.current_tts_rvc.set_index_path("")
                print("Не используем индексную базу для RVC.")

            f0method_override = settings.get(f0method_key, None)

            tts_rate = int(settings.get(tts_rate_key, 0)) if not is_combined_model else 0

            if self.provider in ["NVIDIA"]:
                inference_params = {
                    "pitch": pitch,
                    "f0method": f0method_override,
                    "index_rate": index_rate,
                    "protect": protect,
                    "filter_radius": filter_radius,
                    "rms_mix_rate": rms_mix_rate,
                    "is_half": is_half,
                }
            else:
                inference_params = {
                    "pitch": pitch,
                    "f0method": f0method_override,
                    "index_rate": index_rate,
                    "protect": protect,
                    "filter_radius": filter_radius,
                    "rms_mix_rate": rms_mix_rate
                }
            if f0method_override:
                inference_params["f0method"] = f0method_override

            output_file_rvc = None
            if os.path.abspath(self.pth_path) != os.path.abspath(self.current_tts_rvc.current_model):
                if self.provider in ["NVIDIA"]:
                    self.current_tts_rvc.current_model = self.pth_path
                elif self.provider in ["AMD"]:
                    self.current_tts_rvc.current_model = self.pth_path
                    self.current_tts_rvc.set_model(self.pth_path)

            if not TEST_WITH_DONE_AUDIO:
                print(f"Начинаем генерацию аудио RVC (TTS) для текста: {text[:50]}...")
                if not is_combined_model:
                    inference_params["tts_rate"] = tts_rate
                output_file_rvc = self.current_tts_rvc(text=text, **inference_params)
            else:
                print(f"Обрабатываем существующее аудио RVC: {TEST_WITH_DONE_AUDIO}")
                output_file_rvc = self.current_tts_rvc.voiceover_file(
                    input_path=TEST_WITH_DONE_AUDIO,
                    **inference_params
                )

            print(f"Аудио сгенерировано RVC, путь: {output_file_rvc}")

            if not output_file_rvc or not os.path.exists(output_file_rvc) or os.path.getsize(output_file_rvc) == 0:
                print(f"Внимание: сгенерированный RVC файл {output_file_rvc} отсутствует или имеет нулевой размер!")
                return None
            
            print(f"RVC файл создан успешно, размер: {os.path.getsize(output_file_rvc)} байт")

            stereo_output_file = output_file_rvc.replace(".wav", "_stereo.wav")
            final_output_path = output_file_rvc
            atempo_value = 1.0

            if self.current_model == "low" and TEST_WITH_DONE_AUDIO is None:
                atempo_value = 1.0

            converted_file = await self.convert_wav_to_stereo(output_file_rvc, stereo_output_file, atempo=atempo_value)

            if converted_file and os.path.exists(converted_file):
                print(f"Файл успешно конвертирован в стерео: {stereo_output_file}")
                final_output_path = stereo_output_file
                try:
                    os.remove(output_file_rvc)
                    print(f"Удален промежуточный файл: {output_file_rvc}")
                except OSError as error:
                    print(f"Не удалось удалить промежуточный файл {output_file_rvc}: {error}")
            else:
                print("Не удалось конвертировать файл в стерео формат, используется исходный RVC файл.")

            print(f"Озвучка создана: {final_output_path}")
            if self.parent.ConnectedToGame == True and TEST_WITH_DONE_AUDIO is None:
                self.parent.patch_to_sound_file = final_output_path
                
            return final_output_path
        except Exception as error:
            import traceback
            traceback.print_exc()
            print(f"Ошибка при создании озвучки с Edge-TTS + RVC ({self.current_model}): {error}")
            return None

    def _preprocess_text_to_ssml(self, text: str):
        if not text:
            return "<speak></speak>", 2, "Mila" # Питч и имя по умолчанию

        # --- Определение параметров персонажа (без изменений) ---
        character_rvc_pitch = 2
        character_rate = "medium"
        character_short_name = "Mila"
        character_speaker = "kseniya"
        current_char_obj = getattr(self, 'current_character', None)
        if current_char_obj:
            char_short_name_attr = getattr(current_char_obj, 'short_name', None)
            if char_short_name_attr:
                character_short_name = str(char_short_name_attr)
                if character_short_name == "CappieMita": character_rvc_pitch = 6; character_speaker = "kseniya" 
                elif character_short_name == "MitaKind": character_rvc_pitch = 1; character_speaker = "kseniya" 
                elif character_short_name == "ShorthairMita": character_rvc_pitch = 2; character_speaker = "kseniya" 
                elif character_short_name == "CrazyMita": character_rvc_pitch = 2; character_speaker = "kseniya" 
                elif character_short_name == "Mila": character_rvc_pitch = 2; character_speaker = "kseniya" 
                elif character_short_name == "TinyMita": character_rvc_pitch = -3; character_speaker = "baya" 
                elif character_short_name == "SleepyMita": character_rvc_pitch = 2; character_speaker = "baya" 
                elif character_short_name == "GhostMita": character_rvc_pitch = 1; character_speaker = "baya" 

        # --- Предобработка текста (без изменений) ---
        text = re.sub(r'<[^>]*>', '', text)
        text = escape(text)
        text = text.replace("Мита", "М+ита").replace("Мила", "М+ила")
        text = text.replace("мита", "м+ита").replace("мила", "м+ила")

        split_pattern = r'([.!?]+[^A-Za-zА-Яа-я0-9_]*)(\s+)'
        parts = re.split(split_pattern, text.strip())

        processed_text = ""
        i = 0
        while i < len(parts):
            text_part = parts[i]
            if text_part:
                processed_text += text_part

            if i + 1 < len(parts) and i + 2 < len(parts):
                punctuation_part = parts[i+1]
                whitespace_part = parts[i+2] 

                if punctuation_part:
                    processed_text += punctuation_part

                if whitespace_part and i + 3 < len(parts) and parts[i+3]:
                    processed_text += f' <break time="300ms"/> '

            i += 3

        prosody_attributes = []
        if character_rate != "medium":
            prosody_attributes.append(f'rate="{character_rate}"')

        if prosody_attributes:
            ssml_content = f'<prosody {" ".join(prosody_attributes)}>{processed_text}</prosody>'
        else:
            ssml_content = processed_text

        if ssml_content:
            ssml_output = f'<speak><p>{ssml_content}</p></speak>'
        else:
            ssml_output = '<speak></speak>'

        return ssml_output, character_rvc_pitch, character_speaker  
    
    async def voiceover_silero_rvc(self, text, character=None):
        if self.current_silero_model is None or self.current_tts_rvc is None:
            raise Exception("Модель Silero или RVC не инициализирована для low+")

        self.current_character = character if character is not None else getattr(self, 'current_character', None)

        temp_wav = None
        try:
            ssml_text, character_base_rvc_pitch, character_speaker = self._preprocess_text_to_ssml(text)

            settings = self.load_model_settings("low+")
            if not settings:
                print("Предупреждение: Не найдены настройки для модели low+. Используются значения по умолчанию.")

            silero_put_accent = settings.get("silero_put_accent", True)
            silero_put_yo = settings.get("silero_put_yo", True)
            speaker = character_speaker

            print(f"Начинаем генерацию аудио Silero для SSML: {ssml_text[:100]}...")
            audio_tensor = self.current_silero_model.apply_tts(
                ssml_text=ssml_text,
                speaker=speaker,
                sample_rate=self.current_silero_sample_rate,
                put_accent=silero_put_accent,
                put_yo=silero_put_yo
            )

            audio_numpy = audio_tensor.cpu().numpy()
            temp_wav_file = tempfile.NamedTemporaryFile(suffix=".wav", delete=False)
            temp_wav = temp_wav_file.name
            temp_wav_file.close()
            sf.write(temp_wav, audio_numpy, self.current_silero_sample_rate)

            if not os.path.exists(temp_wav) or os.path.getsize(temp_wav) == 0:
                 print(f"Внимание: сгенерированный Silero файл {temp_wav} отсутствует или имеет нулевой размер!")
                 return None
            print(f"Silero файл создан успешно: {temp_wav}, размер: {os.path.getsize(temp_wav)} байт")

            base_rvc_pitch_from_settings = float(settings.get("pitch", 6))
            final_rvc_pitch = base_rvc_pitch_from_settings - (6 - character_base_rvc_pitch)

            index_rate = float(settings.get("index_rate", 0.75))
            protect = float(settings.get("protect", 0.33))
            filter_radius = int(settings.get("filter_radius", 3))
            rms_mix_rate = float(settings.get("rms_mix_rate", 0.5))
            is_half_str = settings.get("is_half", "True")
            is_half = is_half_str.lower() == "true"
            use_index_file = settings.get("use_index_file", True)
            f0method_override = settings.get("f0method", None)

            # Используем оригинальную логику определения путей на основе аргумента character или дефолта
            active_character_for_rvc_path = character # Используем аргумент функции
            is_nvidia = self.provider in ["NVIDIA"]
            model_ext = 'pth' if is_nvidia else 'onnx'

            if active_character_for_rvc_path is not None:
                # Пытаемся получить short_name из переданного объекта character
                rvc_model_short_name = str(getattr(active_character_for_rvc_path, 'short_name', None))
                if not rvc_model_short_name:
                    print(f"Предупреждение: Не удалось получить short_name из объекта character. Используется имя по умолчанию 'Mila'.")
                    rvc_model_short_name = "Mila"
            else:
                # Если character не передан, используем имя по умолчанию
                rvc_model_short_name = "Mila"

            self.pth_path = os.path.join(os.path.abspath(self.clone_voice_folder), f"{rvc_model_short_name}.{model_ext}")
            self.index_path = os.path.join(os.path.abspath(self.clone_voice_folder), f"{rvc_model_short_name}.index")

            print(f"Выбрана RVC модель: {rvc_model_short_name}, Путь: {self.pth_path}")

            if not os.path.exists(self.pth_path):
                 # Можно добавить обработку ошибки или fallback на дефолтную модель, если основная не найдена
                 raise Exception(f"Файл модели RVC не найден: {self.pth_path}")

            if os.path.abspath(self.pth_path) != os.path.abspath(getattr(self.current_tts_rvc, 'current_model', '')):
                if self.provider in ["NVIDIA"]:
                    self.current_tts_rvc.current_model = self.pth_path
                elif self.provider in ["AMD"]:
                    self.current_tts_rvc.current_model = self.pth_path
                    self.current_tts_rvc.set_model(self.pth_path)

            if use_index_file and self.index_path and os.path.exists(self.index_path):
                self.current_tts_rvc.set_index_path(self.index_path)
                print("Используем индексную базу для RVC:", self.index_path)
            else:
                self.current_tts_rvc.set_index_path("")
                print("Не используем индексную базу для RVC (файл не найден или отключен).")

            rvc_params = {
                "pitch": final_rvc_pitch,
                "index_rate": index_rate,
                "protect": protect,
                "filter_radius": filter_radius,
                "rms_mix_rate": rms_mix_rate,
            }
            if self.provider == "NVIDIA":
                rvc_params["is_half"] = is_half
            if f0method_override:
                rvc_params["f0method"] = f0method_override

            print(f"Текущая модель: {self.current_tts_rvc.current_model}")
            print(f"Обрабатываем аудио RVC: {temp_wav} с питчем {final_rvc_pitch}")
            output_file_rvc = self.current_tts_rvc.voiceover_file(
                input_path=temp_wav,
                **rvc_params
            )

            print(f"Аудио сгенерировано RVC, путь: {output_file_rvc}")

            if not output_file_rvc or not os.path.exists(output_file_rvc) or os.path.getsize(output_file_rvc) == 0:
                print(f"Внимание: сгенерированный RVC файл {output_file_rvc} отсутствует или имеет нулевой размер!")
                return None

            print(f"RVC файл создан успешно, размер: {os.path.getsize(output_file_rvc)} байт")

            stereo_output_file = output_file_rvc.replace(".wav", "_stereo.wav")
            final_output_path = output_file_rvc
            atempo_value = 1.0

            converted_file = await self.convert_wav_to_stereo(output_file_rvc, stereo_output_file, atempo=atempo_value)

            if converted_file and os.path.exists(converted_file):
                print(f"Файл успешно конвертирован в стерео: {stereo_output_file}")
                final_output_path = stereo_output_file
                try:
                    os.remove(output_file_rvc)
                    print(f"Удален промежуточный файл RVC: {output_file_rvc}")
                except OSError as error:
                    print(f"Не удалось удалить промежуточный файл RVC {output_file_rvc}: {error}")
            else:
                print("Не удалось конвертировать файл в стерео формат, используется исходный RVC файл.")

            print(f"Озвучка создана: {final_output_path}")
            if hasattr(self, 'parent') and getattr(self.parent, 'ConnectedToGame', False):
                self.parent.patch_to_sound_file = final_output_path

            return final_output_path

        except Exception as error:
            import traceback
            traceback.print_exc()
            print(f"Ошибка при создании озвучки с Silero + RVC: {error}")
            return None
        finally:
            if temp_wav and os.path.exists(temp_wav):
                try:
                    os.remove(temp_wav)
                    print(f"Удален временный файл Silero: {temp_wav}")
                except OSError as e:
                    print(f"Не удалось удалить временный файл Silero {temp_wav}: {e}")

    async def voiceover_fish_speech(self, text, compile=False, with_rvc=False):
        if self.fish_speech_module is None:
            raise ImportError("Модуль fish_speech_lib не установлен. Сначала установите модель 'medium', 'medium+' или 'medium+low'.")

        try:
            if not self.current_fish_speech:
                print(f"Предупреждение: Экземпляр FishSpeech не инициализирован для модели {self.current_model}. Попытка инициализации...")
                if not self.initialize_model(self.current_model):
                    raise Exception(f"Не удалось инициализировать модель FishSpeech для {self.current_model}")

            settings = self.load_model_settings(self.current_model)
            if not settings:
                print(f"Предупреждение: Не найдены настройки для модели {self.current_model}. Используются значения по умолчанию.")

            is_combined_model = self.current_model == "medium+low"

            temp_key = "fsprvc_fsp_temperature" if is_combined_model else "temperature"
            top_p_key = "fsprvc_fsp_top_p" if is_combined_model else "top_p"
            rep_penalty_key = "fsprvc_fsp_repetition_penalty" if is_combined_model else "repetition_penalty"
            chunk_len_key = "fsprvc_fsp_chunk_length" if is_combined_model else "chunk_length"
            max_tokens_key = "fsprvc_fsp_max_tokens" if is_combined_model else "max_new_tokens"

            temperature = float(settings.get(temp_key, 0.7))
            top_p = float(settings.get(top_p_key, 0.7))
            repetition_penalty = float(settings.get(rep_penalty_key, 1.2))
            chunk_length = int(settings.get(chunk_len_key, 200))
            max_new_tokens = int(settings.get(max_tokens_key, 1024))
            use_memory_cache = True

            reference_audio_path = None
            reference_text = ""
            if self.clone_voice_filename and os.path.exists(self.clone_voice_filename):
                reference_audio_path = self.clone_voice_filename
                print(f"Используем референс аудио: {reference_audio_path}")
                if self.clone_voice_text and os.path.exists(self.clone_voice_text):
                    try:
                        with open(self.clone_voice_text, "r", encoding="utf-8") as file:
                            reference_text = file.read().strip()
                        print(f"Используем референс текст: '{reference_text[:50]}...'")
                    except Exception as error:
                        print(f"Ошибка чтения файла референс текста {self.clone_voice_text}: {error}")
                else:
                    print(f"Предупреждение: Файл референс текста не найден или не указан ({self.clone_voice_text}).")
            else:
                print(f"Предупреждение: Файл референс аудио не найден или не указан ({self.clone_voice_filename}). Генерация без клонирования голоса.")

            print(f"Начинаем генерацию аудио FishSpeech для текста: {text[:50]}...")
            sample_rate, audio_data = self.current_fish_speech(
                text=text,
                reference_audio=reference_audio_path,
                reference_audio_text=reference_text,
                top_p=top_p,
                temperature=temperature,
                repetition_penalty=repetition_penalty,
                max_new_tokens=max_new_tokens,
                chunk_length=chunk_length,
                use_memory_cache=use_memory_cache,
            )

            current_time_str = datetime.now().strftime("%Y%m%d_%H%M%S_%f")
            hash_object = hashlib.sha1(f"{text[:20]}_{current_time_str}".encode())
            random_name = hash_object.hexdigest()[:10]
            temp_dir = "temp"
            os.makedirs(temp_dir, exist_ok=True)
            raw_output_filename = f"fish_raw_{random_name}.wav"
            raw_output_path = os.path.abspath(os.path.join(temp_dir, raw_output_filename))

            import soundfile as sf
            sf.write(raw_output_path, audio_data, sample_rate, format='WAV')

            if not os.path.exists(raw_output_path) or os.path.getsize(raw_output_path) == 0:
                print(f"Внимание: сгенерированный FishSpeech файл {raw_output_path} отсутствует или имеет нулевой размер!")
                return None

            print(f"FishSpeech файл создан успешно: {raw_output_path}, размер: {os.path.getsize(raw_output_path)} байт")

            stereo_output_filename = raw_output_filename.replace("_raw", "_stereo")
            stereo_output_path = os.path.abspath(os.path.join(temp_dir, stereo_output_filename))
            volume_adjustment = 1.5
            converted_file = await self.convert_wav_to_stereo(raw_output_path, stereo_output_path, volume=str(volume_adjustment))

            processed_output_path = raw_output_path

            if converted_file and os.path.exists(converted_file):
                print(f"Файл успешно конвертирован в стерео: {stereo_output_path}")
                processed_output_path = stereo_output_path
                try:
                    os.remove(raw_output_path)
                    print(f"Удален промежуточный файл: {raw_output_path}")
                except OSError as error:
                    print(f"Не удалось удалить промежуточный файл {raw_output_path}: {error}")
            else:
                print("Не удалось конвертировать файл в стерео формат, используется исходный FishSpeech файл.")

            final_output_path = processed_output_path

            if with_rvc:
                print(f"Применяем RVC к файлу: {final_output_path}")
                if not self.current_tts_rvc:
                    print(f"Предупреждение: Экземпляр TTS_RVC не инициализирован для модели {self.current_model}. Попытка инициализации...")
                    if not self.initialize_model(self.current_model):
                        raise Exception(f"Не удалось инициализировать компонент RVC для модели {self.current_model}")

                rvc_output_path = await self.voiceover_edge_tts_rvc(text=None, TEST_WITH_DONE_AUDIO=final_output_path)

                if rvc_output_path and os.path.exists(rvc_output_path):
                    print(f"RVC обработка завершена: {rvc_output_path}")
                    if final_output_path != rvc_output_path:
                        try:
                            os.remove(final_output_path)
                            print(f"Удален промежуточный файл перед RVC: {final_output_path}")
                        except OSError as error:
                            print(f"Не удалось удалить промежуточный файл {final_output_path}: {error}")
                    final_output_path = rvc_output_path
                else:
                    print("Ошибка во время обработки RVC. Возвращается результат до RVC.")

            print(f"Итоговая озвучка создана: {final_output_path}")

            if self.parent and hasattr(self.parent, 'patch_to_sound_file'):
                self.parent.patch_to_sound_file = final_output_path

            return final_output_path

        except Exception as error:
            import traceback
            traceback.print_exc()
            print(f"Ошибка при создании озвучки с Fish Speech ({self.current_model}): {error}")
            return None
        
    async def voiceover_zonos(self, text, output_file="output.wav"):
        """Создает озвучку с помощью Zonos"""
        # Заглушка - в реальном коде здесь будет реализация Zonos
        try:
            # Имитируем работу
            await asyncio.sleep(3)
            self.create_dummy_wav(output_file)
            return output_file
        except Exception as e:
            print(f"Ошибка при создании озвучки с Zonos: {e}")
            return None

    def create_dummy_wav(self, output_file):
        """Создает пустой WAV файл для тестирования"""
        import wave
        import struct
        
        # Создаем пустой WAV файл (1 секунда тишины)
        with wave.open(output_file, 'w') as f:
            f.setnchannels(1)
            f.setsampwidth(2)
            f.setframerate(44100)
            for i in range(44100):
                value = int(0)
                data = struct.pack('<h', value)
                f.writeframesraw(data)

    async def play(self, file_path):
        """Проигрывает WAV файл."""
        if not os.path.exists(file_path):
            print(f"Файл {file_path} не найден")
            return False

        def play_func():
            pygame.mixer.init()
            pygame.mixer.music.load(file_path)
            pygame.mixer.music.play()
            while pygame.mixer.music.get_busy():
                pygame.time.Clock().tick(10)
            pygame.mixer.music.stop()  
            pygame.mixer.quit() 

        # Выполняем блокирующую функцию в отдельном потоке
        await asyncio.to_thread(play_func)
        return True


    def is_model_installed(self, model_id):
        """Проверяет, установлена ли модель по её идентификатору"""
        if model_id == "low":
            return self.is_edge_tts_rvc_installed()
        elif model_id == "low+": 
            return self.is_edge_tts_rvc_installed() # and self.provider in ["NVIDIA"]
        elif model_id == "medium":
            return self.is_fish_speech_installed()
        elif model_id == "medium+":
            return self.is_fish_speech_installed() and self.is_triton_installed()
        elif model_id == "medium+low":
            return self.is_edge_tts_rvc_installed() and self.is_fish_speech_installed() and self.is_triton_installed()

        elif model_id == "high":
            return self.is_zonos_installed()
        return False

    def is_edge_tts_rvc_installed(self):
        """Проверяет, установлен ли Edge-TTS + RVC"""
        return self.tts_rvc_module is not None

    def is_fish_speech_installed(self):
        """Проверяет, установлен ли Fish Speech"""
        return self.fish_speech_module is not None

    def is_zonos_installed(self):
        """Проверяет, установлен ли Zonos"""
        return self.zonos_module is not None 
    
    def is_triton_installed(self):
        """Проверяет, установлен ли Triton"""
        try:
            import triton
            return True
        except ImportError:
            return False
    
    def is_cuda_available(self):
        """Проверяет доступность CUDA"""
        try:
            import torch
            return torch.cuda.is_available()
        except ImportError:
            return False
    
        
    def check_gpu_provider(self):
        # output = subprocess.check_output(
        #     command, text=True, close_fds=True, stdin=subprocess.DEVNULL, stderr=subprocess.PIPE
        # ).strip()
        nvidia_output = subprocess.check_output("wmic path win32_VideoController get name", stdin=subprocess.DEVNULL, stderr=subprocess.PIPE).decode()
        

        if "NVIDIA" in nvidia_output:
            return "NVIDIA" if not self.amd_test else "AMD"
        
        # Check for AMD GPU
        if "AMD" in nvidia_output or "Radeon" in nvidia_output:
            return "AMD"
        
        else:
            return None
        
    #region Settings region:
    def load_model_settings(self, model_id):
        """Загружает настройки для указанной модели из файла настроек"""
        try:
            settings_file = os.path.join("Settings", "voice_model_settings.json")
            if os.path.exists(settings_file):
                with open(settings_file, "r", encoding="utf-8") as f:
                    all_settings = json.load(f)
                    if model_id in all_settings:
                        return all_settings[model_id]
            return {}
        except Exception as e:
            print(f"Ошибка при загрузке настроек модели {model_id}: {e}")
            return {}
    #endregion

    def add_to_initialized(self, model_id):
        self.initialized_models.add(model_id)
        if model_id == "medium+" and "low" in self.initialized_models:
            self.initialized_models.add("medium+low")
        elif model_id == "medium+low":
            self.initialized_models.add("medium+")

    def is_model_initialized(self, model_id):
        """Проверяет, инициализирована ли модель с указанным ID"""
        if model_id in self.initialized_models:
            return True
        return False

    #region Big fucntion to initialize model
    def initialize_model(self, model_id, init=False):
        """Инициализирует модель с указанным ID и параметрами из настроек"""
        if model_id not in ["low", "low+", "medium", "medium+", "medium+low"]: 
            print(f"Неизвестный ID модели: {model_id}")
            return False

        if self.is_model_initialized(model_id):
            return True
        
        # Загружаем настройки для модели
        settings = self.load_model_settings(model_id)
        if not settings and model_id != "low+":
             print(f"Предупреждение: Не найдены настройки для модели {model_id}. Используются значения по умолчанию.")
        
        try:
            if model_id == "low":
                # Edge-TTS + RVC
                if self.tts_rvc_module is None:
                    print("Модуль tts_with_rvc не установлен")
                    return False
                    
                # Получаем параметры из настроек
                device = settings.get("device", "cuda:0" if self.provider == "NVIDIA" else "dml" if self.provider == "AMD" else "cpu")
                f0_method = settings.get("f0method", "rmvpe" if self.provider == "NVIDIA" else "pm")
                
                if (self.pth_path and os.path.exists(self.pth_path)) or os.path.exists("Models\\Mila.pth") or os.path.exists("Models\\Mila.onnx"):
                    # Инициализируем модель с настройками
                    self.current_tts_rvc = None
                    if self.provider == "NVIDIA":
                        self.current_tts_rvc = self.tts_rvc_module(
                            model_path=self.pth_path if self.pth_path else "Models\\Mila.pth",
                            device=device,
                            f0_method=f0_method
                        )
                    elif self.provider == "AMD":
                        self.current_tts_rvc = self.tts_rvc_module(
                            model_path=self.pth_path if self.pth_path else "Models\\Mila.onnx",
                            device=device,
                            f0_method=f0_method
                        )
                    
                    self.current_tts_rvc.set_voice("ru-RU-SvetlanaNeural")
                    
                    if init:
                        init_text = f"Инициализация модели {model_id}"
                        pitch = float(settings.get("pitch", 6))
                        index_rate = float(settings.get("index_rate", 0.75))
                        protect = float(settings.get("protect", 0.33))
                        tts_rate = int(settings.get("tts_rate", 0))
                        # output_gain = float(settings.get("output_gain", 0.75))
                        filter_radius = int(settings.get("filter_radius", 3))
                        rms_mix_rate = float(settings.get("rms_mix_rate", 0.5))
                        is_half = settings.get("is_half", "True") == "True"
                        
                        try:
                            if self.provider == "NVIDIA":
                                self.current_tts_rvc(
                                    text=init_text,
                                    pitch=pitch,
                                    index_rate=index_rate,
                                    protect=protect,
                                    tts_rate=tts_rate,
                                    # output_gain=output_gain,
                                    filter_radius=filter_radius,
                                    rms_mix_rate=rms_mix_rate,
                                    is_half=is_half
                                )
                            else:
                                self.current_tts_rvc(
                                    text=init_text,
                                    pitch=pitch,
                                    index_rate=index_rate,
                                    protect=protect,
                                    tts_rate=tts_rate,
                                    # output_gain=output_gain,
                                    filter_radius=filter_radius,
                                    rms_mix_rate=rms_mix_rate
                                )
                        except Exception as e:
                            print(f"Ошибка при инициализации модели с тестовым текстом: {e}")
                            return False
                    
                    self.current_model = model_id
                    self.add_to_initialized(model_id)
                    return True
                else:
                    print(f"Не найден файл модели по пути: {self.pth_path}")
                    return False
                    
            elif model_id == "low+": # Добавлен блок инициализации low+
                if self.tts_rvc_module is None:
                    print("Модуль tts_with_rvc не установлен (необходим для low+)")
                    return False

                # Инициализация RVC части 
                rvc_device = settings.get("device", "cuda:0" if self.provider == "NVIDIA" else "dml" if self.provider == "AMD" else "cpu")
                rvc_f0_method = settings.get("f0method", "rmvpe" if self.provider == "NVIDIA" else "pm")

                rvc_is_half_str = settings.get("is_half", "True")

                rvc_is_half = rvc_is_half_str.lower() == "true"

                default_model_name = "Mila"

                is_nvidia = self.provider in ["NVIDIA"]

                default_pth_path = os.path.join(os.path.abspath(self.clone_voice_folder), f"{default_model_name}.{'pth' if is_nvidia else 'onnx'}")
                model_path_to_use = default_pth_path

                if self.pth_path and os.path.exists(self.pth_path):
                    model_path_to_use = self.pth_path
                elif not os.path.exists(default_pth_path):
                     print(f"Не найден файл RVC модели по умолчанию: {default_pth_path}")
                     return False

                if self.current_tts_rvc == None:
                    try:
                        self.current_tts_rvc = self.tts_rvc_module(
                            model_path=model_path_to_use, device=rvc_device, f0_method=rvc_f0_method
                        )
                        self.current_tts_rvc.set_voice("ru-RU-SvetlanaNeural")
                        print("RVC компонент для low+ инициализирован.")
                    except Exception as e:
                        print(f"Ошибка инициализации RVC компонента для low+: {e}")
                        traceback.print_exc()
                        return False


                # Инициализация Silero части (загрузка из кэша torch.hub)
                silero_device = settings.get("silero_device", "cuda" if self.provider == "NVIDIA" else "cpu")
                silero_sample_rate = int(settings.get("silero_sample_rate", 48000))
                language = 'ru'
                model_id_silero = 'v4_ru'
                self.current_silero_model = None
                try:
                    import torch
                    print(f"Загрузка модели Silero ({language}/{model_id_silero}) из torch.hub...")

                    model, _ = torch.hub.load(repo_or_dir='snakers4/silero-models',
                                              model='silero_tts',
                                              language=language,
                                              speaker=model_id_silero,
                                              trust_repo=True)

                    # Перемещаем модель на нужное устройство
                    print("SILERO LOCAL: Перемещаем модель на нужное устройство", silero_device)
                    model.to(silero_device)
                    self.current_silero_model = model
                    self.current_silero_sample_rate = silero_sample_rate
                    print(f"Silero компонент для low+ инициализирован на устройстве {silero_device}.")

                except Exception as e:
                    print(f"Ошибка инициализации Silero компонента для low+: {e}")
                    traceback.print_exc()

                    try:
                        print("Попытка очистить кэш torch.hub и загрузить Silero снова...")

                        model, _ = torch.hub.load(repo_or_dir='snakers4/silero-models',
                                                  model='silero_tts',
                                                  language=language,
                                                  speaker=model_id_silero,
                                                  force_reload=True, # Принудительная перезагрузка
                                                  trust_repo=True)
                        
                        print("SILERO LOCAL: Перемещаем модель на нужное устройство", silero_device)
                        model.to(silero_device)
                        self.current_silero_model = model
                        self.current_silero_sample_rate = silero_sample_rate
                        print(f"Silero компонент успешно загружен после перезагрузки на {silero_device}.")
                    except Exception as e2:
                        print(f"Повторная ошибка инициализации Silero компонента для low+: {e2}")
                        traceback.print_exc()
                        return False # Если и вторая попытка не удалась, выходим

                # Если нужна инициализация с тестовым текстом
                if init:
                    init_text = f"Инициализация модели {model_id}"
                    try:
                        # Устанавливаем .index для RVC перед тестом
                        rvc_use_index_file = settings.get("use_index_file", True)
                        index_path_to_use = None
                        if rvc_use_index_file:
                            default_index_path = os.path.join(os.path.abspath(self.clone_voice_folder), f"{default_model_name}.index")
                            if self.index_path and os.path.exists(self.index_path):
                                index_path_to_use = self.index_path
                            elif os.path.exists(default_index_path):
                                index_path_to_use = default_index_path
                        self.current_tts_rvc.set_index_path(index_path_to_use if index_path_to_use else "")

                        # Вызываем voiceover_silero_rvc
                        loop = asyncio.new_event_loop()
                        asyncio.set_event_loop(loop)
                        loop.run_until_complete(self.voiceover_silero_rvc(init_text))
                        loop.close()
                        print(f"Тестовый прогон для {model_id} завершен.")
                    except Exception as e:
                        print(f"Ошибка при инициализации модели {model_id} с тестовым текстом: {e}")
                        traceback.print_exc()
                        # return False # Не прерываем

                self.current_model = model_id
                self.add_to_initialized(model_id)
                print(f"Модель {model_id} успешно инициализирована.")
                return True

            elif model_id == "medium":
                # Fish Speech
                if self.fish_speech_module is None:
                    print("Модуль fish_speech_lib не установлен")
                    return False
                    
                # Получаем параметры из настроек
                device = settings.get("device", "cuda")
                half = settings.get("half", "False") == "True"
                temperature = float(settings.get("temperature", 0.7))
                top_p = float(settings.get("top_p", 0.7))
                repetition_penalty = float(settings.get("repetition_penalty", 1.2))
                chunk_length = int(settings.get("chunk_length", 200))
                max_new_tokens = int(settings.get("max_new_tokens", 1024))
                compile_model = settings.get("compile_model", "False") == "True"
                
                # Инициализируем модель
                self.current_fish_speech = None
                self.current_fish_speech = self.fish_speech_module(
                    device=device,
                    half=half,
                    compile_model=compile_model
                )
                if not self.first_compiled:
                    self.first_compiled = False


                if init and (self.clone_voice_filename and os.path.exists(self.clone_voice_filename) or os.path.exists("Models\\Mila.wav")) :
                    init_text = f"Инициализация модели {model_id}"
                    ref_text = ""
                    if self.clone_voice_text and os.path.exists(self.clone_voice_text):
                        with open(self.clone_voice_text, "r", encoding="utf-8") as f:
                            ref_text = f.read()
                    
                    try:
                        import soundfile as sf
                        sample_rate, audio_data = self.current_fish_speech(
                            init_text,
                            reference_audio=self.clone_voice_filename if self.clone_voice_filename else "Models\\Mila.wav",
                            reference_audio_text=ref_text if self.clone_voice_filename else "Models\\Mila.txt",
                            top_p=top_p,
                            temperature=temperature,
                            repetition_penalty=repetition_penalty,
                            max_new_tokens=min(max_new_tokens, 100),
                            chunk_length=chunk_length,
                            use_memory_cache=False
                        )
                        
                        # Сохраняем результат для проверки
                        import hashlib
                        from datetime import datetime
                        current_time = datetime.now().strftime("%Y-%m-%d %H:%M:%S.%f")
                        hash_object = hashlib.md5(current_time.encode())
                        random_name = hash_object.hexdigest()[:8]
                        output_path = f"temp/init-{random_name}.wav"
                        os.makedirs("temp", exist_ok=True)
                        sf.write(output_path, audio_data, sample_rate, format='WAV')
                    except Exception as e:
                        print(f"Ошибка при инициализации Fish Speech с тестовым текстом: {e}")
                        return False
                
                self.current_model = model_id
                self.add_to_initialized(model_id)
                return True
                
            elif model_id == "medium+":
                # Fish Speech+
                if self.fish_speech_module is None:
                    print("Модуль fish_speech_lib не установлен")
                    return False
                    
                if not self.is_triton_installed():
                    print("Модуль triton не установлен, требуется для Fish Speech+")
                    return False
                    
                # Получаем параметры из настроек
                device = settings.get("device", "cuda")
                half = settings.get("half", "True") == "True"
                temperature = float(settings.get("temperature", 0.7))
                top_p = float(settings.get("top_p", 0.8))
                repetition_penalty = float(settings.get("repetition_penalty", 1.1))
                chunk_length = int(settings.get("chunk_length", 200))
                max_new_tokens = int(settings.get("max_new_tokens", 1024))
                compile_model = settings.get("compile_model", "True") == "True"
                
                # Инициализируем модель с компиляцией
                try:
                    self.current_fish_speech = None
                    self.current_fish_speech = self.fish_speech_module(
                        device=device,
                        half=half,
                        compile_model=compile_model
                    )
                    if not self.first_compiled:
                        self.first_compiled = True
                    
                    if init and (self.clone_voice_filename and os.path.exists(self.clone_voice_filename) or os.path.exists("Models\\Mila.wav")):
                        init_text = f"Инициализация модели {model_id}"
                        ref_text = ""
                        if self.clone_voice_text and os.path.exists(self.clone_voice_text):
                            with open(self.clone_voice_text, "r", encoding="utf-8") as f:
                                ref_text = f.read()
                        
                        try:
                            import soundfile as sf
                            sample_rate, audio_data = self.current_fish_speech(
                                init_text,
                                reference_audio=self.clone_voice_filename if self.clone_voice_filename else "Models\\Mila.wav",
                                reference_audio_text=ref_text if self.clone_voice_filename else "Models\\Mila.txt",
                                top_p=top_p,
                                temperature=temperature,
                                repetition_penalty=repetition_penalty,
                                max_new_tokens=min(max_new_tokens, 100),
                                chunk_length=chunk_length,
                                use_memory_cache=False
                            )
                            
                            # Сохраняем результат для проверки
                            import hashlib
                            from datetime import datetime
                            current_time = datetime.now().strftime("%Y-%m-%d %H:%M:%S.%f")
                            hash_object = hashlib.md5(current_time.encode())
                            random_name = hash_object.hexdigest()[:8]
                            output_path = f"temp/init-{random_name}.wav"
                            os.makedirs("temp", exist_ok=True)
                            sf.write(output_path, audio_data, sample_rate, format='WAV')
                        except Exception as e:
                            print(f"Ошибка при инициализации Fish Speech+ с тестовым текстом: {e}")
                            return False
                    
                    self.current_model = model_id
                    self.add_to_initialized(model_id)
                    return True
                except Exception as e:
                    print(f"Ошибка при инициализации Fish Speech+: {e}")
                    return False
                    
            elif model_id == "medium+low":
                # Fish Speech+ + RVC
                if self.fish_speech_module is None or self.tts_rvc_module is None:
                    print("Не хватает необходимых модулей для medium+low")
                    return False
                    
                if not self.is_triton_installed():
                    print("Модуль triton не установлен, требуется для Fish Speech+")
                    return False
                    
                # Получаем параметры для Fish Speech+ из настроек
                fsp_device = settings.get("fsprvc_fsp_device", "cuda")
                fsp_half = settings.get("fsprvc_fsp_half", "True") == "True"
                fsp_temperature = float(settings.get("fsprvc_fsp_temperature", 0.7))
                fsp_top_p = float(settings.get("fsprvc_fsp_top_p", 0.8))
                fsp_repetition_penalty = float(settings.get("fsprvc_fsp_repetition_penalty", 1.1))
                fsp_chunk_length = int(settings.get("fsprvc_fsp_chunk_length", 200))
                fsp_max_tokens = int(settings.get("fsprvc_fsp_max_tokens", 1024))
                
                # Получаем параметры для RVC из настроек
                rvc_device = settings.get("fsprvc_rvc_device", "cuda:0" if self.provider == "NVIDIA" else "dml")
                rvc_is_half = settings.get("fsprvc_is_half", "True") == "True"
                rvc_f0method = settings.get("fsprvc_f0method", "rmvpe" if self.provider == "NVIDIA" else "dio")
                rvc_pitch = float(settings.get("fsprvc_rvc_pitch", 0))
                rvc_index_rate = float(settings.get("fsprvc_index_rate", 0.75))
                rvc_protect = float(settings.get("fsprvc_protect", 0.33))
                # rvc_output_gain = float(settings.get("fsprvc_output_gain", 0.75))
                rvc_filter_radius = int(settings.get("fsprvc_filter_radius", 3))
                rvc_rms_mix_rate = float(settings.get("fsprvc_rvc_rms_mix_rate", 0.5))
                
                # Инициализируем Fish Speech+
                try:
                    self.current_fish_speech = None
                    self.current_fish_speech = self.fish_speech_module(
                        device=fsp_device,
                        half=fsp_half,
                        compile_model=True
                    )

                    if not self.first_compiled:
                        self.first_compiled = True
                    
                    # Инициализируем RVC, если у нас есть путь к модели
                    if (self.pth_path and os.path.exists(self.pth_path)) or os.path.exists("Models\\Mila.pth"):
                        self.current_tts_rvc = self.tts_rvc_module(
                            model_path=self.pth_path if self.pth_path else "Models\\Mila.pth",
                            device=rvc_device,
                            f0_method=rvc_f0method
                        )
                    else:
                        print("Не удалось найти файл модели RVC")
                        return False
                    
                    # Если нужна инициализация с тестовым текстом
                    if init and (self.clone_voice_filename and os.path.exists(self.clone_voice_filename)) or os.path.exists("Models\\Mila.wav"):
                        init_text = f"Инициализация модели {model_id}"
                        ref_text = ""
                        if self.clone_voice_text and os.path.exists(self.clone_voice_text):
                            with open(self.clone_voice_text, "r", encoding="utf-8") as f:
                                ref_text = f.read()
                        else:
                            if os.path.exists("Models\\Mila.txt"):
                                with open("Models\\Mila.txt", "r", encoding="utf-8") as f:
                                    ref_text = f.read()

                        
                        try:
                            import soundfile as sf
                            sample_rate, audio_data = self.current_fish_speech(
                                init_text,
                                reference_audio=self.clone_voice_filename if self.clone_voice_filename else "Models\\Mila.wav",
                                reference_audio_text=ref_text if self.clone_voice_filename else "Models\\Mila.txt",
                                top_p=fsp_top_p,
                                temperature=fsp_temperature,
                                repetition_penalty=fsp_repetition_penalty,
                                max_new_tokens=min(fsp_max_tokens, 100),
                                chunk_length=fsp_chunk_length,
                                use_memory_cache=False
                            )
                            
                            import hashlib
                            from datetime import datetime
                            current_time = datetime.now().strftime("%Y-%m-%d %H:%M:%S.%f")
                            hash_object = hashlib.md5(current_time.encode())
                            random_name = hash_object.hexdigest()[:8]
                            temp_output_path = f"temp/init-fsp-{random_name}.wav"
                            os.makedirs("temp", exist_ok=True)
                            sf.write(temp_output_path, audio_data, sample_rate, format='WAV')
                            

                            if not self.pth_path and os.path.exists("Models\\Mila.pth"):
                                self.current_tts_rvc.current_voice = "Models\\Mila.pth"
                            else:
                                self.current_tts_rvc.current_voice = self.pth_path
                            output_path = self.current_tts_rvc.voiceover_file(
                                input_path=temp_output_path,
                                pitch=rvc_pitch,
                                index_rate=rvc_index_rate,
                                protect=rvc_protect,
                                # output_gain=rvc_output_gain,
                                filter_radius=rvc_filter_radius,
                                rms_mix_rate=rvc_rms_mix_rate,
                                is_half=rvc_is_half
                            )
                            
                            # Проверяем результат
                            if not os.path.exists(output_path):
                                print(f"Ошибка при инициализации RVC: файл {output_path} не создан")
                                return False
                        except Exception as e:
                            print(f"Ошибка при инициализации Fish Speech+ + RVC: {e}")
                            return False
                    
                    self.current_model = model_id
                    self.add_to_initialized(model_id)
                    return True
                except Exception as e:
                    print(f"Ошибка при инициализации Fish Speech+ + RVC: {e}")
                    return False
        
        except Exception as e:
            import traceback
            traceback.print_exc()
            print(f"Непредвиденная ошибка при инициализации модели {model_id}: {e}")
            return False
    #endregion

    
    