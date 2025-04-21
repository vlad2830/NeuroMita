# voice_model_settings.py

import tkinter as tk
from tkinter import ttk
import os
import platform
import time
import copy
import json
import threading 
import tkinter.messagebox
from docs import DocsManager

model_descriptions = {
    "low": "Быстрая модель, комбинация Edge-TTS для генерации речи и RVC для преобразования голоса. Низкие требования.",
    "low+": "Комбинация Silero TTS для генерации речи и RVC для преобразования голоса. Требования схожи с low.",
    "medium": "Модель Fish Speech для генерации речи с хорошим качеством. Требует больше ресурсов.",
    "medium+": "Улучшенная версия Fish Speech с потенциально более высоким качеством или доп. возможностями. Требует больше места.",
    "medium+low": "Комбинация Fish Speech+ и RVC для высококачественного преобразования голоса. Самые высокие требования."
}

setting_descriptions = {
    "device": "Устройство для вычислений (GPU или CPU). 'cuda:0' - первая GPU NVIDIA, 'cpu' - центральный процессор, 'mps:0' - GPU Apple Silicon.",
    "pitch": "Изменение высоты голоса в полутонах. Положительные значения - выше, отрицательные - ниже. 0 - без изменений.",
    "is_half": "Использовать вычисления с половинной точностью (float16). Ускоряет работу на совместимых GPU (NVIDIA RTX и новее) и экономит видеопамять, может незначительно повлиять на качество.",
    "output_gain": "Усиление громкости финального аудиофайла. Значение 1.0 - без изменений, <1 тише, >1 громче. Полезно для нормализации громкости разных голосов.",
    "f0method": "[RVC] Алгоритм извлечения основного тона (F0). Определяет высоту голоса. 'rmvpe' и 'crepe' точные, но медленные. 'pm', 'harvest' быстрее. Влияет на естественность интонаций.",
    "use_index_file": "[RVC] Использовать файл .index для улучшения соответствия тембра голоса модели. Отключение может быть полезно, если индексный файл низкого качества или вызывает артефакты.",
    "index_rate": "[RVC] Степень использования индексного файла (.index) для сохранения тембра голоса RVC (0 до 1). Выше значение = больше похоже на голос из модели, но может добавить артефакты, если индекс некачественный.",
    "filter_radius": "[RVC] Радиус медианного фильтра для сглаживания кривой F0 (высоты тона). Убирает резкие скачки, делает речь более плавной. Рекомендуется значение >= 3.",
    "rms_mix_rate": "[RVC] Степень смешивания RMS (громкости) исходного аудио (из TTS/FSP) и результата RVC (0 до 1). 0 = полностью громкость RVC, 1 = полностью громкость оригинала. Помогает сохранить исходную динамику речи.",
    "protect": "[RVC] Защита глухих согласных (ш, щ, с, ...) от искажения высотой тона (0 до 0.5). Меньшие значения обеспечивают большую защиту (согласные звучат чище), но могут немного повлиять на интонацию гласных рядом. Рекомендуется 0.3-0.4.",
    "tts_rate": "[EdgeTTS] Изменение скорости речи базового синтезатора Edge-TTS (до RVC) в процентах. 0 - стандартная скорость.",
    "tts_volume": "[EdgeTTS] Изменение громкости речи базового синтезатора Edge-TTS (до RVC) в процентах. 0 - стандартная громкость.",
    "silero_device": "[Silero] Устройство для генерации речи Silero (CPU или GPU).",
    "silero_sample_rate": "[Silero] Частота дискретизации для генерации речи Silero.", 
    "silero_put_accent": "[Silero] Автоматическая расстановка ударений.", 
    "silero_put_yo": "[Silero] Автоматическая замена 'е' на 'ё'.", 
    "half": "[FS/FSP] Использовать FP16 (половинную точность). Рекомендуется для скорости и экономии памяти на GPU.",
    "temperature": "[FS/FSP] Температура сэмплирования (>0). Контролирует случайность/разнообразие генерируемой речи. Выше = разнообразнее, но больше ошибок. Ниже = стабильнее.",
    "top_p": "[FS/FSP] Ядерное сэмплирование (Top-P, 0-1). Ограничивает выбор следующего токена только наиболее вероятными вариантами. Уменьшает вероятность генерации 'бреда'.",
    "repetition_penalty": "[FS/FSP] Штраф за повторение токенов (>1). Предотвращает зацикливание модели на одних и тех же словах/звуках. 1.0 - нет штрафа.",
    "chunk_length": "[FS/FSP] Размер чанка обработки текста (в токенах). Влияет на использование памяти и длину контекста, который модель 'видит' одновременно.",
    "max_new_tokens": "[FS/FSP] Максимальное количество генерируемых токенов за один шаг. Ограничивает длину аудиофрагмента, генерируемого за раз.",
    "compile_model": "[FSP] Использовать torch.compile() для JIT-компиляции модели. Значительно ускоряет выполнение на GPU после первого запуска, но требует доп. установки и время на компиляцию при старте.",
    "fsprvc_fsp_device": "[FSP+RVC][FSP] Устройство для части Fish Speech+.",
    "fsprvc_fsp_half": "[FSP+RVC][FSP] Half-precision для части Fish Speech+.",
    "fsprvc_fsp_temperature": "[FSP+RVC][FSP] Температура для части Fish Speech+.",
    "fsprvc_fsp_top_p": "[FSP+RVC][FSP] Top-P для части Fish Speech+.",
    "fsprvc_fsp_repetition_penalty": "[FSP+RVC][FSP] Штраф повторений для части Fish Speech+.",
    "fsprvc_fsp_chunk_length": "[FSP+RVC][FSP] Размер чанка для части Fish Speech+.",
    "fsprvc_fsp_max_tokens": "[FSP+RVC][FSP] Макс. токены для части Fish Speech+.",
    "fsprvc_rvc_device": "[FSP+RVC][RVC] Устройство для части RVC.",
    "fsprvc_is_half": "[FSP+RVC][RVC] Half-precision для части RVC.",
    "fsprvc_f0method": "[FSP+RVC][RVC] Метод F0 для части RVC.",
    "fsprvc_rvc_pitch": "[FSP+RVC][RVC] Высота голоса для части RVC.",
    "fsprvc_use_index_file": "[FSP+RVC][RVC] Использовать файл .index для улучшения соответствия тембра голоса модели. Отключение может быть полезно, если индексный файл низкого качества или вызывает артефакты.",
    "fsprvc_index_rate": "[FSP+RVC][RVC] Соотношение индекса для части RVC.",
    "fsprvc_protect": "[FSP+RVC][RVC] Защита согласных для части RVC.",
    "fsprvc_output_gain": "[FSP+RVC][RVC] Громкость (gain) для части RVC.",
    "fsprvc_filter_radius": "[FSP+RVC][RVC] Радиус фильтра F0 для части RVC.",
    "fsprvc_rvc_rms_mix_rate": "[FSP+RVC][RVC] Смешивание RMS для части RVC.",
    "tmp_directory": "Папка для временных файлов, создаваемых в процессе работы (например, промежуточные аудиофайлы).",
    "verbose": "Включить вывод подробной отладочной информации в консоль для диагностики проблем.",
    "cuda_toolkit": "Наличие установленного CUDA Toolkit от NVIDIA. Необходимо для некоторых функций (например, torch.compile) и работы с GPU NVIDIA.",
    "windows_sdk": "Наличие установленного Windows SDK. Может требоваться для компиляции некоторых зависимостей Python.",
}

default_description_text = "Наведите курсор на элемент интерфейса для получения описания."

try:
    from utils.GpuUtils import check_gpu_provider, get_cuda_devices, get_gpu_name_by_id
except ImportError:
    print("Предупреждение: Модуль GpuUtils не найден. Функции определения GPU не будут работать.")
    def check_gpu_provider(): return None
    def get_cuda_devices(): return []


class VoiceCollapsibleSection(ttk.Frame): # Используем ttk.Frame для стиля
    def __init__(self, parent, title, collapsed=False, update_scrollregion_func=None, clear_description_func=None, **kwargs):
        super().__init__(parent, **kwargs)

        self.configure(style='Collapsible.TFrame')
        self.update_scrollregion = update_scrollregion_func
        self.clear_description = clear_description_func or (lambda event=None: None)

        self.border_frame = tk.Frame(self, bg="#1e1e1e") 
        self.border_frame.pack(fill=tk.X, expand=True, pady=(0, 1))
        self.header_frame = tk.Frame(self.border_frame, bg="#252525")
        self.header_frame.pack(fill=tk.X, side=tk.TOP, anchor="nw", pady=(2, 1))
        self.arrow = tk.Label(self.header_frame, text=("▶" if collapsed else "▼"), bg="#252525", fg="white", width=2, font=("Segoe UI", 8)) # Шрифт для стрелки
        self.arrow.pack(side=tk.LEFT, padx=(5, 5))
        self.title_label = tk.Label(self.header_frame, text=title, bg="#252525", fg="white", font=("Segoe UI", 9, "bold")) # Жирный шрифт
        self.title_label.pack(side=tk.LEFT, pady=2)

        # Контентный фрейм - tk, т.к. внутри него grid
        self.content_frame = tk.Frame(self.border_frame, bg="#1e1e1e")
        self.content_frame.columnconfigure(0, weight=4, uniform="group1", minsize=150)
        self.content_frame.columnconfigure(1, weight=5, uniform="group1", minsize=150)

        for widget in [self.arrow, self.title_label, self.header_frame]:
            widget.bind("<Button-1>", self.toggle)
            widget.bind("<Leave>", self.clear_description)

        self.is_collapsed = collapsed
        self.row_count = 0
        self.widgets = {} # Словарь для хранения виджетов и их переменных

        if self.is_collapsed: self.collapse(update_scroll=False)
        else: self.expand(update_scroll=False)

    def toggle(self, event=None):
        if self.is_collapsed: self.expand()
        else: self.collapse()
        self.is_collapsed = not self.is_collapsed
        if self.update_scrollregion: self.after(10, self.update_scrollregion)

    def collapse(self, update_scroll=True):
        self.arrow.config(text="▶")
        self.content_frame.pack_forget()
        if update_scroll and self.update_scrollregion: self.after(10, self.update_scrollregion)

    def expand(self, update_scroll=True):
        self.arrow.config(text="▼")
        self.content_frame.pack(fill=tk.X, side=tk.TOP, padx=1, pady=(0, 1))
        if update_scroll and self.update_scrollregion: self.after(10, self.update_scrollregion)

    def add_row(self, key, label_text, widget_type, options, setting_info, show_setting_description=None):
        row_height = 28
        is_locked = setting_info.get("locked", False)
        label_fg_color = "#888888" if is_locked else "white"
        # Состояния для виджетов
        widget_state_tk = tk.DISABLED if is_locked else tk.NORMAL
        widget_state_ttk = "disabled" if is_locked else "normal"
        combobox_state_ttk = "disabled" if is_locked else "readonly"

        # Контейнеры и метка (tk)
        label_container = tk.Frame(self.content_frame, bg="#252525", height=row_height)
        label_container.grid(row=self.row_count, column=0, sticky="nsew", pady=(1, 0))
        label_container.pack_propagate(False)
        label = tk.Label(label_container, text=label_text, bg="#252525", fg=label_fg_color, anchor="w", font=("Segoe UI", 9))
        label.pack(fill=tk.BOTH, expand=True, padx=10, pady=3)

        widget_container = tk.Frame(self.content_frame, bg="#1e1e1e", height=row_height)
        widget_container.grid(row=self.row_count, column=1, sticky="nsew", pady=(1, 0))
        widget_container.pack_propagate(False)

        widget = None
        widget_var = None 
        container_padx = 5
        container_pady = 2
        entry_bg = "#3c3c3c"
        widget_fg = "white"
        disabled_fg = "#888888"
        disabled_bg = "#303030"

        current_value = options.get("default")

        if widget_type == "entry":
            entry_var = tk.StringVar()
            entry = tk.Entry(widget_container, textvariable=entry_var, bg=entry_bg, fg=widget_fg,
                           disabledbackground=disabled_bg, disabledforeground=disabled_fg,
                           insertbackground=widget_fg,
                           relief="solid", bd=1, highlightthickness=0, state=widget_state_tk, font=("Segoe UI", 9))
            entry.pack(fill=tk.X, expand=True, padx=container_padx, pady=container_pady, ipady=2)
            if current_value is not None:
                entry_var.set(str(current_value))
            widget = entry
            widget_var = entry_var

        elif widget_type == "combobox":
            combo_var = tk.StringVar()
            values_list = options.get("values", [])
            if not isinstance(values_list, (list, tuple)): values_list = []

            # Используем ttk.Combobox с кастомным стилем
            combo = ttk.Combobox(
                widget_container,
                textvariable=combo_var,
                values=values_list,
                state=combobox_state_ttk,
                style='CustomDark.TCombobox', # Применяем наш стиль
                font=("Segoe UI", 9)
            )
            combo.pack(fill=tk.X, expand=True, padx=container_padx, pady=container_pady, ipady=1)

            # Функция установки начального значения
            def set_initial_combo_value(target_widget, possible_values, value_to_set):
                if not target_widget.winfo_exists(): return
                if value_to_set is not None:
                    str_value_to_set = str(value_to_set)
                    str_possible_values = [str(v) for v in possible_values]
                    try:
                        value_index = str_possible_values.index(str_value_to_set)
                        target_widget.current(value_index)
                        return
                    except ValueError: pass
                    except tk.TclError: pass

                if possible_values:
                    try: target_widget.current(0)
                    except tk.TclError: pass

            combo.after_idle(lambda w=combo, v=values_list[:], cv=current_value: set_initial_combo_value(w, v, cv))

            widget = combo
            widget_var = combo_var

        elif widget_type == "checkbutton":
            var = tk.BooleanVar()
            bool_value = False
            if isinstance(current_value, str):
                bool_value = current_value.lower() == 'true'
            elif current_value is not None:
                bool_value = bool(current_value)
            var.set(bool_value)

            # Используем ttk.Checkbutton с кастомным стилем
            check = ttk.Checkbutton(
                widget_container, variable=var,
                style='CustomDark.TCheckbutton',
                state=widget_state_ttk,
                takefocus=False
            )
            check.pack(side=tk.RIGHT, padx=container_padx + 10, pady=container_pady)
            widget = check
            widget_var = var

        if widget:
            # Сохраняем виджет и его переменную
            self.widgets[key] = {'widget': widget, 'variable': widget_var}
            if show_setting_description:
                elements_to_bind = [label_container, label, widget_container, widget]
                for w in elements_to_bind:
                     if w:
                         w.bind("<Enter>", lambda event, k=key: show_setting_description(k), '+')
                         w.bind("<Leave>", self.clear_description, '+')

        self.row_count += 1
        return widget

    def get_values(self):
        values = {}
        for key, data in self.widgets.items():
            widget = data.get('widget')
            variable = data.get('variable')
            value = None
            try:
                if isinstance(widget, ttk.Combobox):
                    value = widget.get()
                elif isinstance(widget, tk.Entry) and variable:
                    value = variable.get()
                elif isinstance(widget, ttk.Checkbutton) and variable:
                    value = variable.get()
                elif variable:
                     value = variable.get()
                elif isinstance(widget, tk.Entry):
                     value = widget.get()
                values[key] = value
            except Exception as e:
                print(f"Ошибка получения значения для {key}: {e}")
                values[key] = None
        return values

class VoiceModelSettingsWindow:
    _ttk_styles_initialized = False # Флаг для однократной инициализации стилей

    def __init__(self, master=None, config_dir=None, on_save_callback=None, local_voice=None, check_installed_func=None):
        self.master = master or tk.Tk()
        self.master.title("Настройки и Установка Локальных Моделей")
        self.master.minsize(750, 500)
        self.master.geometry("875x800")
        self.master.configure(bg="#1e1e1e")

        self.local_voice = local_voice
        self.check_installed_func = check_installed_func
        self.config_dir = config_dir or os.path.dirname(os.path.abspath(__file__))
        self.settings_values_file = os.path.join(self.config_dir, "voice_model_settings.json")
        self.installed_models_file = os.path.join(self.config_dir, "installed_models.txt")
        self.on_save_callback = on_save_callback

        self.model_descriptions = model_descriptions
        self.setting_descriptions = setting_descriptions
        self.default_description_text = default_description_text

        self.detected_gpu_vendor = check_gpu_provider()
        self.detected_cuda_devices = []
        self.gpu_name = None # Инициализируем имя GPU

        if self.detected_gpu_vendor == "NVIDIA":
            self.detected_cuda_devices = get_cuda_devices()
            if self.detected_cuda_devices:
                # Пытаемся получить имя первой CUDA видеокарты
                try:
                    first_device_id = self.detected_cuda_devices[0]
                    self.gpu_name = get_gpu_name_by_id(first_device_id)
                    if self.gpu_name:
                        print(f"Обнаружена GPU: {self.gpu_name}")
                    else:
                        print(f"Не удалось получить имя для GPU {first_device_id}")
                except Exception as e:
                    print(f"Предупреждение: Не удалось получить имя GPU: {e}")

        self.description_label_widget = None
        self.settings_sections = {}
        self.download_buttons = {}
        self.installed_models = set()
        self.scrollable_frame_settings = None
        self.placeholder_label_settings = None
        self.top_frame_settings = None
        self.models_canvas = None
        self.settings_canvas = None
        self.models_scrollable_area = None
        self.local_voice_models = []

        self.load_settings()
        
        self.docs_manager = DocsManager()
        self._check_system_dependencies()

        self._initialize_custom_ttk_styles()

        self._initialize_layout()
        self._create_model_panels()
        self.display_installed_models_settings()

        self.master.after(100, self._update_settings_scrollregion)
        self.master.after(100, self._update_models_scrollregion)

    def _initialize_custom_ttk_styles(self):
        if VoiceModelSettingsWindow._ttk_styles_initialized:
            return

        style = ttk.Style()

        # Цвета
        frame_bg = "#1e1e1e"
        widget_bg = "#3c3c3c"
        widget_fg = "white"
        border_color = "#6b6b6b"
        darker_border_color = "#444444"
        select_bg = "#005f87"
        disabled_fg = "#888888"
        disabled_bg = "#303030"
        arrow_color = widget_fg
        arrow_active_bg = "#4f4f4f"
        # Цвета для Scrollbar
        scrollbar_trough_color = frame_bg 
        scrollbar_thumb_color = "#555555"
        scrollbar_thumb_active_color = "#6a6a6a"
        scrollbar_border_color = darker_border_color 

        # --- Стиль Combobox (CustomDark.TCombobox) ---
        style.element_create("CustomDarkCombobox.field", "from", "default")
        style.element_create("CustomDarkCombobox.downarrow", "from", "default")
        style.layout("CustomDark.TCombobox", [(
            'CustomDarkCombobox.field', {'sticky': 'nswe', 'border': '1', 'children': [(
                    'CustomDarkCombobox.textarea', {'sticky': 'nswe'}
            )]}) , (
            'CustomDarkCombobox.downarrow', {'side': 'right', 'sticky': 'ns'}
        )])
        style.configure("CustomDark.TCombobox",
                        background=widget_bg, fieldbackground=widget_bg, foreground=widget_fg,
                        bordercolor=darker_border_color, arrowcolor=arrow_color, arrowsize=12,
                        borderwidth=1, padding=(5, 4), relief="solid", insertcolor=widget_fg)
        style.map("CustomDark.TCombobox",
                  foreground=[('disabled', disabled_fg)],
                  fieldbackground=[('disabled', disabled_bg)],
                  bordercolor=[('disabled', darker_border_color), ('focus', select_bg), ('hover', border_color)],
                  arrowcolor=[('disabled', disabled_fg), ('pressed', arrow_color), ('hover', arrow_color)])

        # --- Стиль Listbox внутри Combobox (через option_add) ---
        self.master.option_add('*TCombobox*Listbox.background', widget_bg)
        self.master.option_add('*TCombobox*Listbox.foreground', widget_fg)
        self.master.option_add('*TCombobox*Listbox.selectBackground', select_bg)
        self.master.option_add('*TCombobox*Listbox.selectForeground', widget_fg)
        self.master.option_add('*TCombobox*Listbox.font', ("Segoe UI", 9))
        self.master.option_add('*TCombobox*Listbox.borderWidth', 1)
        self.master.option_add('*TCombobox*Listbox.borderColor', darker_border_color)
        self.master.option_add('*TCombobox*Listbox.relief', 'solid')
        self.master.option_add('*TCombobox*Listbox.highlightThickness', 0)

        # --- Стиль Checkbutton (CustomDark.TCheckbutton) ---
        style.configure('CustomDark.TCheckbutton',
                        background=frame_bg, foreground=widget_fg, indicatorcolor=widget_bg,
                        indicatorrelief='solid', indicatormargin=2, indicatordiameter=12,
                        padding=5, relief='flat', font=("Segoe UI", 9))
        style.map('CustomDark.TCheckbutton',
                  indicatorcolor=[('selected', select_bg), ('active', '#555555')])


        style.configure('Collapsible.TFrame', background=frame_bg)

        style.element_create("CustomScrollbar.trough", "from", "default") 
        style.element_create("CustomScrollbar.thumb", "from", "default") 

        style.layout('CustomDark.Vertical.TScrollbar', [(
            'CustomScrollbar.trough', {'sticky': 'ns', 'children': [(
                'CustomScrollbar.thumb', {'expand': '1', 'sticky': 'nswe'}
            )]}
        )])
        # Убираем стрелки, если они есть в базовой теме
        style.layout('CustomDark.Horizontal.TScrollbar', [(
            'CustomScrollbar.trough', {'sticky': 'ew', 'children': [(
                'CustomScrollbar.thumb', {'expand': '1', 'sticky': 'nswe'}
            )]}
        )])


        style.configure('CustomDark.Vertical.TScrollbar',
                        relief='flat',
                        borderwidth=0,
                        background=scrollbar_trough_color,
                        troughrelief='flat',
                        troughborderwidth=0,
                        troughcolor=scrollbar_trough_color
                        )
        style.configure('CustomDark.Horizontal.TScrollbar', # На всякий случай, если понадобится
                        relief='flat',
                        borderwidth=0,
                        background=scrollbar_trough_color,
                        troughrelief='flat',
                        troughborderwidth=0,
                        troughcolor=scrollbar_trough_color
                        )

        # Настройка ручки (thumb)
        style.configure('CustomDark.Vertical.TScrollbar',   thumbrelief='flat', thumbborderwidth=0, background=scrollbar_thumb_color, bordercolor=scrollbar_border_color)
        style.configure('CustomDark.Horizontal.TScrollbar', thumbrelief='flat', thumbborderwidth=0, background=scrollbar_thumb_color, bordercolor=scrollbar_border_color)

        # Настройка ручки при наведении/нажатии
        style.map('CustomDark.Vertical.TScrollbar',
                  background=[('active', scrollbar_thumb_active_color), # Цвет ручки при наведении/нажатии
                              ('disabled', scrollbar_trough_color)], # Цвет ручки, если скроллбар неактивен
                  troughcolor=[('disabled', scrollbar_trough_color)]
                 )
        style.map('CustomDark.Horizontal.TScrollbar',
                  background=[('active', scrollbar_thumb_active_color),
                              ('disabled', scrollbar_trough_color)],
                  troughcolor=[('disabled', scrollbar_trough_color)]
                 )


        VoiceModelSettingsWindow._ttk_styles_initialized = True

    def get_default_model_structure(self):
        return [
             {
                "id": "low", "name": "Edge-TTS + RVC", "min_vram": 3, "rec_vram": 4,
                "gpu_vendor": ["NVIDIA", "AMD"], "size_gb": 3,
                "settings": [
                    {"key": "device", "label": "Устройство RVC", "type": "combobox", "options": { "values_nvidia": ["dml", "cuda:0", "cpu"], "default_nvidia": "cuda:0", "values_amd": ["dml", "cpu"], "default_amd": "dml", "values_other": ["cpu", "mps:0"], "default_other": "cpu" }},
                     {"key": "is_half", "label": "Half-precision RVC", "type": "combobox", "options": {"values": ["True", "False"], "default_nvidia": "True", "default_amd": "False", "default_other": "False"}},
                    {"key": "f0method", "label": "Метод F0 (RVC)", "type": "combobox", "options": { "values_nvidia": ["pm", "rmvpe", "crepe", "harvest", "fcpe", "dio"], "default_nvidia": "rmvpe", "values_amd": ["rmvpe", "harvest", "pm", "dio"], "default_amd": "pm", "values_other": ["pm", "rmvpe", "crepe", "harvest", "fcpe", "dio"], "default_other": "pm" }},
                    {"key": "pitch", "label": "Высота голоса RVC (пт)", "type": "entry", "options": {"default": "6"}},
                    {"key": "use_index_file", "label": "Исп. .index файл (RVC)", "type": "checkbutton", "options": {"default": True}},
                    {"key": "index_rate", "label": "Соотношение индекса RVC", "type": "entry", "options": {"default": "0.75"}},
                    {"key": "protect", "label": "Защита согласных (RVC)", "type": "entry", "options": {"default": "0.33"}},
                    {"key": "tts_rate", "label": "Скорость TTS (%)", "type": "entry", "options": {"default": "0"}},
                    # {"key": "output_gain", "label": "Громкость RVC (gain)", "type": "entry", "options": {"default": "0.75"}},
                    {"key": "filter_radius", "label": "Радиус фильтра F0 (RVC)", "type": "entry", "options": {"default": "3"}},
                    {"key": "rms_mix_rate", "label": "Смешивание RMS (RVC)", "type": "entry", "options": {"default": "0.5"}},
                ]
            },
            { # Добавлена новая модель low+
                "id": "low+", "name": "Silero + RVC", "min_vram": 3, "rec_vram": 4,
                "gpu_vendor": ["NVIDIA", "AMD"], "size_gb": 3,
                "settings": [
                    # Настройки RVC (скопированы из low)
                    {"key": "device", "label": "Устройство RVC", "type": "combobox", "options": { "values_nvidia": ["dml", "cuda:0", "cpu"], "default_nvidia": "cuda:0", "values_amd": ["dml", "cpu"], "default_amd": "dml", "values_other": ["cpu", "dml"], "default_other": "cpu" }},
                    {"key": "is_half", "label": "Half-precision RVC", "type": "combobox", "options": {"values": ["True", "False"], "default_nvidia": "True", "default_amd": "False", "default_other": "False"}},
                    {"key": "f0method", "label": "Метод F0 (RVC)", "type": "combobox", "options": { "values_nvidia": ["pm", "rmvpe", "crepe", "harvest", "fcpe", "dio"], "default_nvidia": "rmvpe", "values_amd": ["rmvpe", "harvest", "pm", "dio"], "default_amd": "pm", "values_other": ["pm", "rmvpe", "harvest", "dio"], "default_other": "pm" }},
                    {"key": "pitch", "label": "Высота голоса RVC (пт)", "type": "entry", "options": {"default": "6"}},
                    {"key": "use_index_file", "label": "Исп. .index файл (RVC)", "type": "checkbutton", "options": {"default": True}},
                    {"key": "index_rate", "label": "Соотношение индекса RVC", "type": "entry", "options": {"default": "0.75"}},
                    {"key": "protect", "label": "Защита согласных (RVC)", "type": "entry", "options": {"default": "0.33"}},
                    {"key": "filter_radius", "label": "Радиус фильтра F0 (RVC)", "type": "entry", "options": {"default": "3"}},
                    {"key": "rms_mix_rate", "label": "Смешивание RMS (RVC)", "type": "entry", "options": {"default": "0.5"}},
                    # Настройки Silero
                    {"key": "silero_device", "label": "Устройство Silero", "type": "combobox", "options": {"values_nvidia": ["cuda", "cpu"], "default_nvidia": "cuda", "values_amd": ["cpu"], "default_amd": "cpu", "values_other": ["cpu"], "default_other": "cpu"}},
                    {"key": "silero_sample_rate", "label": "Частота Silero", "type": "combobox", "options": {"values": ["48000", "24000", "16000"], "default": "48000"}},
                    {"key": "silero_put_accent", "label": "Акценты Silero", "type": "checkbutton", "options": {"default": True}},
                    {"key": "silero_put_yo", "label": "Буква Ё Silero", "type": "checkbutton", "options": {"default": True}}
                ]
            },
            {
                "id": "medium", "name": "Fish Speech", "min_vram": 4, "rec_vram": 6, "gpu_vendor": ["NVIDIA"], "size_gb": 5,
                 "settings": [
                    {"key": "device", "label": "Устройство", "type": "combobox", "options": {"values": ["cuda", "cpu", "mps"], "default": "cuda"}},
                    {"key": "half", "label": "Half-precision", "type": "combobox", "options": {"values": ["False", "True"], "default": "False"}},
                    {"key": "temperature", "label": "Температура", "type": "entry", "options": {"default": "0.7"}},
                    {"key": "top_p", "label": "Top-P", "type": "entry", "options": {"default": "0.7"}},
                    {"key": "repetition_penalty", "label": "Штраф повторений", "type": "entry", "options": {"default": "1.2"}},
                    {"key": "chunk_length", "label": "Размер чанка (~символов)", "type": "entry", "options": {"default": "200"}},
                    {"key": "max_new_tokens", "label": "Макс. токены", "type": "entry", "options": {"default": "1024"}},
                    { "key": "compile_model", "label": "Компиляция модели", "type": "combobox", "options": {"values": ["False", "True"], "default": "False"}, "locked": True}
                ]
            },
            {
                "id": "medium+", "name": "Fish Speech+", "min_vram": 4, "rec_vram": 6, "gpu_vendor": ["NVIDIA"], "size_gb": 10,
                "rtx30plus": True,
                 "settings": [
                    {"key": "device", "label": "Устройство", "type": "combobox", "options": {"values": ["cuda", "cpu", "mps"], "default": "cuda"}},
                    {"key": "half", "label": "Half-precision", "type": "combobox", "options": {"values": ["True", "False"], "default": "False"}, "locked": True},
                    {"key": "temperature", "label": "Температура", "type": "entry", "options": {"default": "0.7"}},
                    {"key": "top_p", "label": "Top-P", "type": "entry", "options": {"default": "0.8"}},
                    {"key": "repetition_penalty", "label": "Штраф повторений", "type": "entry", "options": {"default": "1.1"}},
                    {"key": "chunk_length", "label": "Размер чанка (~символов)", "type": "entry", "options": {"default": "200"}},
                    {"key": "max_new_tokens", "label": "Макс. токены", "type": "entry", "options": {"default": "1024"}},
                    { "key": "compile_model", "label": "Компиляция модели", "type": "combobox", "options": {"values": ["False", "True"], "default": "True"}, "locked": True}
                 ]
            },
            {
                "id": "medium+low", "name": "Fish Speech+ + RVC", "min_vram": 6, "rec_vram": 8, "gpu_vendor": ["NVIDIA"], "size_gb": 15,
                "rtx30plus": True,
                "settings": [
                    {"key": "fsprvc_fsp_device", "label": "[FSP] Устройство", "type": "combobox", "options": {"values": ["cuda", "cpu", "mps"], "default": "cuda"}},
                    {"key": "fsprvc_fsp_half", "label": "[FSP] Half-precision", "type": "combobox", "options": {"values": ["True", "False"], "default": "False"}, "locked": True},
                    {"key": "fsprvc_fsp_temperature", "label": "[FSP] Температура", "type": "entry", "options": {"default": "0.7"}},
                    {"key": "fsprvc_fsp_top_p", "label": "[FSP] Top-P", "type": "entry", "options": {"default": "0.7"}},
                    {"key": "fsprvc_fsp_repetition_penalty", "label": "[FSP] Штраф повторений", "type": "entry", "options": {"default": "1.2"}},
                    {"key": "fsprvc_fsp_chunk_length", "label": "[FSP] Размер чанка (слов)", "type": "entry", "options": {"default": "200"}},
                    {"key": "fsprvc_fsp_max_tokens", "label": "[FSP] Макс. токены", "type": "entry", "options": {"default": "1024"}},
                    {"key": "fsprvc_rvc_device", "label": "[RVC] Устройство", "type": "combobox", "options": {"values": ["cuda:0", "cpu", "mps:0", "dml"], "default_nvidia": "cuda:0", "default_amd": "dml"}},
                    {"key": "fsprvc_is_half", "label": "[RVC] Half-precision", "type": "combobox", "options": {"values": ["True", "False"], "default_nvidia": "True", "default_amd": "False"}},
                    {"key": "fsprvc_f0method", "label": "[RVC] Метод F0", "type": "combobox", "options": {"values": ["pm", "rmvpe", "crepe", "harvest", "fcpe", "dio"], "default_nvidia": "rmvpe", "default_amd": "dio"}},
                    {"key": "fsprvc_rvc_pitch", "label": "[RVC] Высота голоса (пт)", "type": "entry", "options": {"default": "0"}},
                    {"key": "fsprvc_use_index_file", "label": "[RVC] Исп. .index файл", "type": "checkbutton", "options": {"default": True}},
                    {"key": "fsprvc_index_rate", "label": "[RVC] Соотн. индекса", "type": "entry", "options": {"default": "0.75"}},
                    {"key": "fsprvc_protect", "label": "[RVC] Защита согласных", "type": "entry", "options": {"default": "0.33"}},
                    # {"key": "fsprvc_output_gain", "label": "[RVC] Громкость (gain)", "type": "entry", "options": {"default": "0.75"}},
                    {"key": "fsprvc_filter_radius", "label": "[RVC] Радиус фильтра F0", "type": "entry", "options": {"default": "3"}},
                    {"key": "fsprvc_rvc_rms_mix_rate", "label": "[RVC] Смешивание RMS", "type": "entry", "options": {"default": "0.5"}}
                ]
            }
        ]

    def load_settings(self):
        self.installed_models = set()
        if self.local_voice and self.check_installed_func:
            for model_data in self.get_default_model_structure():
                model_id = model_data.get("id")
                if model_id:
                    if model_id == "low" and self.check_installed_func("tts_with_rvc"): self.installed_models.add(model_id)
                    elif model_id == "low+" and self.check_installed_func("tts_with_rvc"): self.installed_models.add(model_id)
                    elif model_id == "medium" and self.check_installed_func("fish_speech_lib"): self.installed_models.add(model_id)
                    elif model_id == "medium+" and self.check_installed_func("fish_speech_lib") and self.check_installed_func("triton"): self.installed_models.add(model_id)
                    elif model_id == "medium+low" and self.check_installed_func("tts_with_rvc") and self.check_installed_func("fish_speech_lib") and self.check_installed_func("triton"): self.installed_models.add(model_id)

        else:
            try:
                if os.path.exists(self.installed_models_file):
                    with open(self.installed_models_file, "r", encoding="utf-8") as f:
                        self.installed_models.update(line.strip() for line in f if line.strip())
            except Exception as e:
                print(f"Ошибка загрузки состояния установленных моделей из {self.installed_models_file}: {e}")

        # 2. Получение базовой структуры по умолчанию
        default_model_structure = self.get_default_model_structure()

        # 3. Адаптация базовой структуры под окружение (GPU/OS) перед загрузкой сохраненных значений
        adapted_default_structure = self.finalize_model_settings(
            default_model_structure, self.detected_gpu_vendor, self.detected_cuda_devices
        )

        # 4. Загрузка сохраненных пользовательских значений из JSON
        saved_values = {}
        try:
            if os.path.exists(self.settings_values_file):
                with open(self.settings_values_file, "r", encoding="utf-8") as f:
                    saved_values = json.load(f)
        except Exception as e:
            print(f"Ошибка загрузки сохраненных значений из {self.settings_values_file}: {e}")
            saved_values = {}

        # 5. Создание финальной структуры: начинаем с адаптированных дефолтов
        merged_model_structure = copy.deepcopy(adapted_default_structure)

        # 6. Наложение сохраненных значений поверх адаптированных дефолтов
        for model_data in merged_model_structure:
            model_id = model_data.get("id")
            if model_id in saved_values:
                model_saved_values = saved_values[model_id]
                if isinstance(model_saved_values, dict):
                    for setting in model_data.get("settings", []):
                        setting_key = setting.get("key")
                        if setting_key in model_saved_values:
                            setting.setdefault("options", {})["default"] = model_saved_values[setting_key]

        self.local_voice_models = merged_model_structure
        print("Загрузка и адаптация настроек завершена.") 

    def save_settings(self):
        try:
            with open(self.installed_models_file, "w", encoding="utf-8") as f:
                for model_id in sorted(list(self.installed_models)):
                    f.write(f"{model_id}\n")
        except Exception as e:
            print(f"Ошибка сохранения состояния установленных моделей в {self.installed_models_file}: {e}")

        settings_to_save = {}
        for model_id, section in self.settings_sections.items():
            if model_id in self.installed_models and section.winfo_exists():
                try:
                    settings_to_save[model_id] = section.get_values()
                except Exception as e:
                    print(f"Ошибка при сборе значений из UI для модели '{model_id}': {e}")

        if settings_to_save:
            try:
                with open(self.settings_values_file, "w", encoding="utf-8") as f:
                    json.dump(settings_to_save, f, indent=4, ensure_ascii=False)
            except Exception as e:
                print(f"Ошибка сохранения значений настроек в {self.settings_values_file}: {e}")

        if self.on_save_callback:
            callback_data = {
                 "installed_models": list(self.installed_models),
                 "models_data": self.local_voice_models
            }
            self.on_save_callback(callback_data)

    def finalize_model_settings(self, models_list, detected_vendor, cuda_devices):
        final_models = copy.deepcopy(models_list)

        gpu_name_upper = self.gpu_name.upper() if self.gpu_name else ""
        force_fp32 = False
        if detected_vendor == "NVIDIA" and gpu_name_upper:
            if (
                ("16" in gpu_name_upper and "V100" not in gpu_name_upper)
                or "P40" in gpu_name_upper
                or "P10" in gpu_name_upper
                or "1060" in gpu_name_upper # Используем 'in' как в оригинальном условии
                or "1070" in gpu_name_upper
                or "1080" in gpu_name_upper
            ):
                print(f"Обнаружена GPU {self.gpu_name}, принудительно используется FP32 для совместимых настроек.")
                force_fp32 = True
        elif detected_vendor == "AMD":
            force_fp32 = True


        for model in final_models:
            model_vendors = model.get("gpu_vendor", [])
            vendor_to_adapt_for = None
            if detected_vendor == "NVIDIA" and "NVIDIA" in model_vendors: vendor_to_adapt_for = "NVIDIA"
            elif detected_vendor == "AMD" and "AMD" in model_vendors: vendor_to_adapt_for = "AMD"
            elif not detected_vendor or detected_vendor not in model_vendors: vendor_to_adapt_for = "OTHER"
            elif detected_vendor in model_vendors: vendor_to_adapt_for = detected_vendor

            for setting in model.get("settings", []):
                options = setting.get("options", {})
                setting_key = setting.get("key")
                widget_type = setting.get("type")
                is_device_setting = "device" in str(setting_key).lower()
                is_half_setting = setting_key in ["is_half", "fsprvc_is_half", "half", "fsprvc_fsp_half"] # Включаем все варианты half

                final_values_list = None
                adapt_key_suffix = ""
                if vendor_to_adapt_for == "NVIDIA": adapt_key_suffix = "_nvidia"
                elif vendor_to_adapt_for == "AMD": adapt_key_suffix = "_amd"
                elif vendor_to_adapt_for == "OTHER": adapt_key_suffix = "_other"

                values_key = f"values{adapt_key_suffix}"
                default_key = f"default{adapt_key_suffix}"

                if values_key in options: final_values_list = options[values_key]
                elif "values" in options: final_values_list = options["values"]

                if default_key in options: options["default"] = options[default_key]

                if vendor_to_adapt_for == "NVIDIA" and is_device_setting:
                    base_nvidia_values = options.get("values_nvidia", [])
                    base_other_values = options.get("values_other", ["cpu"])
                    base_non_cuda_provider = base_nvidia_values if base_nvidia_values else base_other_values
                    non_cuda_options = [v for v in base_non_cuda_provider if not str(v).startswith("cuda")]
                    if cuda_devices: final_values_list = cuda_devices + non_cuda_options
                    else: final_values_list = [v for v in base_other_values if v in ["cpu", "mps"]] or ["cpu"]

                if final_values_list is not None and widget_type == "combobox":
                    options["values"] = final_values_list

                keys_to_remove = [k for k in options if k.startswith("values_") or k.startswith("default_")]
                for key_to_remove in keys_to_remove: options.pop(key_to_remove, None)

                # Применяем принудительное FP32 и блокировку, если нужно
                if force_fp32 and is_half_setting:
                     options["default"] = "False" # Принудительно ставим False
                     setting["locked"] = True     # Блокируем настройку
                     print(f"  - Принудительно '{setting_key}' = False и заблокировано.")
                elif is_half_setting:
                    print(f"  - '{setting_key}' = True - Доступен.")

                if widget_type == "combobox" and "default" in options and "values" in options:
                    current_values = options["values"]
                    if isinstance(current_values, list):
                        current_default = options["default"]
                        str_values = [str(v) for v in current_values]
                        str_default = str(current_default)
                        if str_default not in str_values:
                            options["default"] = str_values[0] if str_values else ""
                    else:
                         options["default"] = ""
        return final_models

    def get_model_parameters(self, model_id):
        parameters = {}
        settings_file = self.settings_values_file
        if not os.path.exists(settings_file): return parameters
        try:
            with open(settings_file, "r", encoding="utf-8") as f:
                all_saved_data = json.load(f)
            if isinstance(all_saved_data, dict):
                parameters = all_saved_data.get(model_id, {})
        except Exception as e:
            print(f"Ошибка чтения файла настроек {settings_file}: {e}")
        return parameters

    # --- ИСПРАВЛЕННЫЙ МЕТОД ИНИЦИАЛИЗАЦИИ ---
    def _initialize_layout(self):
        main_app_frame = tk.Frame(self.master, bg="#1e1e1e")
        main_app_frame.pack(side=tk.TOP, fill=tk.BOTH, expand=True)
        
        # --- Левая панель ---
        left_panel_frame = tk.Frame(main_app_frame, bg="#1e1e1e", width=280)
        left_panel_frame.pack(side=tk.LEFT, fill=tk.Y, padx=(10, 5), pady=10)
        left_panel_frame.pack_propagate(False)
        # ... (код описания и списка моделей остается прежним) ...
        description_frame = tk.Frame(left_panel_frame, bg="#252525", bd=1, relief=tk.SOLID, height=135, highlightbackground="#444", highlightthickness=1)
        description_frame.pack(side=tk.TOP, fill=tk.X, pady=(0, 10))
        description_frame.pack_propagate(False)
        description_title = tk.Label(description_frame, text="Описание:", font=("Segoe UI", 9, "bold"), anchor="nw", bg="#252525", fg="white")
        description_title.pack(padx=10, pady=(5,2), anchor="nw")
        self.description_label_widget = tk.Label(description_frame, text=self.default_description_text, font=("Segoe UI", 9), anchor="nw", justify="left", wraplength=250, bg="#252525", fg="#cccccc")
        self.description_label_widget.pack(padx=10, pady=(0, 10), fill=tk.BOTH, expand=True)

        models_title = tk.Label(left_panel_frame, text="Доступные Модели:", font=("Segoe UI", 10, "bold"), anchor="nw", bg="#1e1e1e", fg="white")
        models_title.pack(fill=tk.X, pady=(0, 5))
        
        models_canvas_frame = tk.Frame(left_panel_frame, bg="#1e1e1e")
        models_canvas_frame.pack(fill=tk.BOTH, expand=True)
        self.models_canvas = tk.Canvas(models_canvas_frame, bg="#1e1e1e", highlightthickness=0)
        models_scrollbar = ttk.Scrollbar(models_canvas_frame, orient="vertical", command=self.models_canvas.yview, style='CustomDark.Vertical.TScrollbar')
        self.models_canvas.configure(yscrollcommand=models_scrollbar.set)
        models_scrollbar.pack(side=tk.RIGHT, fill=tk.Y)
        self.models_canvas.pack(side=tk.LEFT, fill=tk.BOTH, expand=True)

        self.models_scrollable_area = tk.Frame(self.models_canvas, bg="#1e1e1e")
        models_canvas_window_id = self.models_canvas.create_window((0, 0), window=self.models_scrollable_area, anchor="nw")
        
        self.models_scrollable_area.bind("<Configure>", self._update_models_scrollregion)
        self.models_canvas.bind("<Configure>", lambda e, cw=models_canvas_window_id: self.models_canvas.itemconfig(cw, width=e.width))
        
        self._bind_mousewheel(self.models_canvas, lambda e: self._handle_mousewheel(self.models_canvas, e))
        self._bind_mousewheel(self.models_scrollable_area, lambda e: self._handle_mousewheel(self.models_canvas, e))

        # --- Правая панель ---
        right_settings_frame = tk.Frame(main_app_frame, bg="#1e1e1e")
        right_settings_frame.pack(side=tk.RIGHT, fill=tk.BOTH, expand=True, padx=(5, 10), pady=10)
        
        self.settings_canvas = tk.Canvas(right_settings_frame, bg="#1e1e1e", highlightthickness=0)
        settings_scrollbar = ttk.Scrollbar(right_settings_frame, orient=tk.VERTICAL, command=self.settings_canvas.yview, style='CustomDark.Vertical.TScrollbar')
        self.settings_canvas.configure(yscrollcommand=settings_scrollbar.set)
        settings_scrollbar.pack(side=tk.RIGHT, fill=tk.Y)
        self.settings_canvas.pack(side=tk.LEFT, fill=tk.BOTH, expand=True)

        self.scrollable_frame_settings = tk.Frame(self.settings_canvas, bg="#1e1e1e")
        settings_canvas_window_id = self.settings_canvas.create_window((0, 0), window=self.scrollable_frame_settings, anchor="nw")
        
        self.scrollable_frame_settings.bind("<Configure>", self._update_settings_scrollregion)
        self.settings_canvas.bind("<Configure>", lambda e, cw=settings_canvas_window_id: self.settings_canvas.itemconfig(cw, width=e.width))
        
        self._bind_mousewheel(self.settings_canvas, lambda e: self._handle_mousewheel(self.settings_canvas, e))
        self._bind_mousewheel(self.scrollable_frame_settings, lambda e: self._handle_mousewheel(self.settings_canvas, e))


        # --- Отображение статуса зависимостей (Внутри правой панели) ---
        self.top_frame_settings = tk.Frame(self.scrollable_frame_settings, bg="#1e1e1e")
        self.top_frame_settings.pack(fill=tk.X, padx=10, pady=(10, 5), anchor='nw')

        status_font = ("Segoe UI", 9)
        warning_font = ("Segoe UI", 9, "bold")
        status_label_color = "white"
        status_found_color = "lightgreen"
        status_notfound_color = "#FF6A6A" 
        link_color = "#81d4fa" 
        warning_color = "orange"
        bg_color = "#1e1e1e"

        # Фрейм для ГОРИЗОНТАЛЬНОЙ строки статусов компонентов
        status_items_display_frame = tk.Frame(self.top_frame_settings, bg=bg_color)
        status_items_display_frame.pack(anchor='w', fill=tk.X) # Заполняет ширину

        # Фрейм для строки предупреждения и ссылки (под статусами)
        warning_link_frame = None 

        if platform.system() == "Windows":
            if self.triton_installed:
                if self.triton_checks_performed:
                    # --- ГОРИЗОНТАЛЬНОЕ РАЗМЕЩЕНИЕ СТАТУСОВ ---
                    items = [
                        ("CUDA Toolkit:", self.cuda_found),
                        ("Windows SDK:", self.winsdk_found),
                        ("MSVC:", self.msvc_found)
                    ]
                    
                    # Пакуем каждый статус-блок горизонтально
                    for text, found in items:
                        # Создаем отдельный фрейм для каждой пары (текст + статус)
                        item_frame = tk.Frame(status_items_display_frame, bg=bg_color)
                        # Пакуем эти фреймы слева направо с отступом между ними
                        item_frame.pack(side=tk.LEFT, padx=(0, 15)) # Отступ справа от каждого блока

                        # Внутри item_frame пакуем метку и статус тоже слева направо
                        label = tk.Label(item_frame, text=text, font=status_font, bg=bg_color, fg=status_label_color)
                        label.pack(side=tk.LEFT) 

                        status_text = "Найден" if found else "Не найден"
                        status_color = status_found_color if found else status_notfound_color
                        status_label = tk.Label(item_frame, text=status_text, font=status_font, bg=bg_color, fg=status_color)
                        status_label.pack(side=tk.LEFT, padx=(3, 0)) # Небольшой отступ слева от статуса

                    # Проверка необходимости предупреждения (ПОСЛЕ статусов)
                    if not (self.cuda_found and self.winsdk_found and self.msvc_found):
                        # Создаем и пакуем фрейм для предупреждения/ссылки под статусами
                        warning_link_frame = tk.Frame(self.top_frame_settings, bg=bg_color)
                        warning_link_frame.pack(side=tk.TOP, fill=tk.X, pady=(5, 0), anchor='w') 

                        warning_text = "⚠️ Для моделей Fish Speech+ / +RVC могут потребоваться все компоненты."
                        warning_label = tk.Label(warning_link_frame, text=warning_text, bg=bg_color, fg=warning_color, font=warning_font)
                        warning_label.pack(side=tk.LEFT, padx=(0, 5)) 

                        doc_link_label = tk.Label(warning_link_frame, text="[Документация]", bg=bg_color, fg=link_color, font=warning_font, cursor="hand2")
                        doc_link_label.pack(side=tk.LEFT) 
                        doc_link_label.bind("<Button-1>", lambda event: self.docs_manager.open_doc("installation_guide.html"))

                else: # Ошибка проверки
                    tk.Label(status_items_display_frame, text="⚠️ Ошибка проверки зависимостей Triton.", font=status_font, bg=bg_color, fg=warning_color).pack(anchor='w')
                    # Ссылку можно добавить аналогично, создав warning_link_frame

            else: # Triton не установлен
                 # Фрейм для сообщения и ссылки (будет под status_items_display_frame, т.к. он пустой)
                 warning_link_frame = tk.Frame(self.top_frame_settings, bg=bg_color)
                 warning_link_frame.pack(side=tk.TOP, fill=tk.X, pady=(0, 0), anchor='w') 

                 tk.Label(warning_link_frame, text="Triton не установлен (необходим для Fish Speech+ / +RVC).", font=status_font, bg=bg_color, fg=warning_color).pack(side=tk.LEFT, padx=(0, 5))

                #  doc_link_label_triton = tk.Label(warning_link_frame, text="[Инструкция]", bg=bg_color, fg=link_color, font=status_font, cursor="hand2")
                #  doc_link_label_triton.pack(side=tk.LEFT)
                #  doc_link_label_triton.bind("<Button-1>", lambda event: self.docs_manager.open_doc("installation_guide.html"))

        else: # Не Windows
            tk.Label(status_items_display_frame, text="Проверка зависимостей Triton доступна только в Windows.", font=status_font, bg=bg_color, fg="#aaaaaa").pack(anchor='w')

        bottom_frame = tk.Frame(self.master, bg="#1e1e1e", height=50)
        bottom_frame.pack(side=tk.BOTTOM, fill=tk.X, pady=(0, 10), padx=10)
        bottom_frame.pack_propagate(False) 
        
        save_button = tk.Button(bottom_frame, text="Сохранить", command=self.save_and_continue, bg="#3c3cac", fg="white", activebackground="#4a4acb", activeforeground="white", relief="flat", bd=0, padx=20, pady=5, font=("Segoe UI", 9))
        save_button.pack(side=tk.RIGHT, pady=5, padx=(5, 0))
        
        close_button = tk.Button(bottom_frame, text="Закрыть", command=self.save_and_quit, bg="#3c3c3c", fg="white", activebackground="#555555", activeforeground="white", relief="flat", bd=0, padx=20, pady=5, font=("Segoe UI", 9))
        close_button.pack(side=tk.RIGHT, pady=5, padx=(0, 0)) 

    def _bind_mousewheel(self, widget, command):
         widget.bind("<MouseWheel>", command, '+')
         if platform.system() == "Linux":
             widget.bind("<Button-4>", command, '+')
             widget.bind("<Button-5>", command, '+')

    def _handle_mousewheel(self, canvas, event):
        if not canvas or not canvas.winfo_exists(): return
        canvas.update_idletasks()
        bbox = canvas.bbox("all")
        if not bbox or (bbox[3] - bbox[1]) <= canvas.winfo_height(): return
        delta = 0
        system = platform.system()
        if system == "Windows": delta = -1 * (event.delta // 120)
        elif system == "Darwin": delta = -1 * event.delta
        else: delta = -1 if event.num == 4 else (1 if event.num == 5 else 0)
        if delta != 0: canvas.yview_scroll(delta, "units")

    def _create_model_panels(self):
        if not self.models_scrollable_area: return
        for widget in self.models_scrollable_area.winfo_children(): widget.destroy()
        self.download_buttons = {}
        for model_info in self.local_voice_models:
            panel = self.create_model_panel(self.models_scrollable_area, model_info)
            panel.pack(pady=4, padx=2, fill=tk.X)
        self.master.after(50, self._update_models_scrollregion)

    def display_installed_models_settings(self):
        """Отображает настройки для установленных моделей БЕЗ дополнительной плашки RTX в настройках."""
        if not self.scrollable_frame_settings or not self.scrollable_frame_settings.winfo_exists():
            return

        widgets_to_manage = [w for w in self.scrollable_frame_settings.winfo_children() if w != self.top_frame_settings]
        for widget in widgets_to_manage:
            widget.destroy()

        self.settings_sections.clear()

        if self.placeholder_label_settings and self.placeholder_label_settings.winfo_exists():
            self.placeholder_label_settings.pack_forget()

        if not self.installed_models:
            if self.placeholder_label_settings is None or not self.placeholder_label_settings.winfo_exists():
                 self.placeholder_label_settings = tk.Label(self.scrollable_frame_settings, text="Модели не установлены.\n\nНажмите 'Установить' слева для установки модели,\nее настройки появятся здесь.", font=("Segoe UI", 10), bg="#1e1e1e", fg="#aaa", justify="center", wraplength=350)
            self.placeholder_label_settings.pack(pady=30, padx=10, fill=tk.BOTH, expand=True, after=self.top_frame_settings)
        else:
            any_settings_shown = False
            last_packed_widget = self.top_frame_settings

            for model_data in self.local_voice_models:
                model_id = model_data.get("id")
                if not model_id or model_id not in self.installed_models:
                    continue

                any_settings_shown = True
                model_name = model_data.get('name', model_id)

                section_title = f"Настройки: {model_name}"
                start_collapsed = len(self.installed_models) > 2
                section = VoiceCollapsibleSection(
                    self.scrollable_frame_settings,
                    section_title,
                    collapsed=start_collapsed,
                    update_scrollregion_func=self._update_settings_scrollregion,
                    clear_description_func=self.clear_description
                )
                self.settings_sections[model_id] = section

                # Добавление строк настроек
                model_settings = model_data.get("settings", [])
                current_row_index = 0 # Начинаем с 0 строки внутри секции
                if model_settings:
                    for setting_info in model_settings:
                        key = setting_info.get("key")
                        label = setting_info.get("label", key)
                        widget_type = setting_info.get("type")
                        options = setting_info.get("options", {})
                        if key and widget_type:
                            section.add_row(
                                key, label, widget_type, options, setting_info,
                                show_setting_description=self.show_setting_description
                            )
                else:
                     no_settings_label = tk.Label(section.content_frame, text="Специфические настройки отсутствуют.", bg="#1e1e1e", fg="#ccc", font=("Segoe UI", 9))
                     
                     no_settings_label.grid(row=0, column=0, columnspan=2, pady=10, padx=10)
                     section.row_count = 1


                section.pack(fill=tk.X, padx=10, pady=(5,0), anchor='nw', after=last_packed_widget)
                last_packed_widget = section

            if self.installed_models and not any_settings_shown:
                 if self.placeholder_label_settings is None or not self.placeholder_label_settings.winfo_exists():
                     self.placeholder_label_settings = tk.Label(self.scrollable_frame_settings, text="Не удалось отобразить настройки для установленных моделей.", font=("Segoe UI", 10), bg="#1e1e1e", fg="#aaa", justify="center", wraplength=350)
                 self.placeholder_label_settings.pack(pady=30, padx=10, fill=tk.BOTH, expand=True, after=self.top_frame_settings)


        self.master.after(50, self._update_settings_scrollregion)

        # Замени этот метод в классе VoiceModelSettingsWindow
    def _check_system_dependencies(self):
        """Проверяет наличие CUDA, Windows SDK и MSVC с помощью triton (исправлено)."""
        self.cuda_found = False
        self.winsdk_found = False
        self.msvc_found = False
        self.triton_installed = False
        self.triton_checks_performed = False # Флаг, что проверка была выполнена

        if platform.system() != "Windows":
            print("Проверка зависимостей Triton актуальна только для Windows.")
            return

        try:
            from triton.windows_utils import find_cuda, find_winsdk, find_msvc
            self.triton_installed = True

            # --- Проверка CUDA ---
            cuda_result = find_cuda()
            print(f"CUDA find_cuda() result: {cuda_result}")
            if isinstance(cuda_result, (tuple, list)) and len(cuda_result) >= 1:
                cuda_path = cuda_result[0]
                self.cuda_found = cuda_path is not None and os.path.exists(str(cuda_path)) 
            else:
                self.cuda_found = False
            print(f"CUDA Check: Found={self.cuda_found}")

            # --- Проверка WinSDK ---
            winsdk_result = find_winsdk(False)
            print(f"WinSDK find_winsdk() result: {winsdk_result}") 
            if isinstance(winsdk_result, (tuple, list)) and len(winsdk_result) >= 1:
                winsdk_paths = winsdk_result[0]
                self.winsdk_found = isinstance(winsdk_paths, list) and bool(winsdk_paths)
            else:
                self.winsdk_found = False
            print(f"WinSDK Check: Found={self.winsdk_found}")

            # --- Проверка MSVC ---
            msvc_result = find_msvc(False)
            print(f"MSVC find_msvc() result: {msvc_result}")
            if isinstance(msvc_result, (tuple, list)) and len(msvc_result) >= 1:
                msvc_paths = msvc_result[0]
                self.msvc_found = isinstance(msvc_paths, list) and bool(msvc_paths)
            else:
                self.msvc_found = False
            print(f"MSVC Check: Found={self.msvc_found}")

            self.triton_checks_performed = True

        except ImportError:
            print("Triton не установлен. Невозможно проверить зависимости CUDA/WinSDK/MSVC.")
            self.triton_installed = False
        except Exception as e:
            print(f"Ошибка при проверке зависимостей Triton: {e}")

    # Адаптированный метод create_model_panel
    def create_model_panel(self, parent, model_data):
        panel_bg = "#2a2a2e"
        text_color = "#b0b0b0"
        title_color = "#ffffff"
        border_color = "#444444"
        button_bg = "#555555"
        button_fg = "white"
        button_active_bg = "#666666"
        button_disabled_fg = "#999999"
        warning_color_amd = "#FF6A6A"
        # --- Цвета для значка RTX ---
        rtx_warning_color = "orange"  # Оранжевый (если GPU не подходит)
        rtx_ok_color = "lightgreen" # Зеленый (если GPU подходит)

        model_id = model_data["id"]
        model_name = model_data["name"]
        supported_vendors = model_data.get('gpu_vendor', [])
        requires_rtx30plus = model_data.get("rtx30plus", False)

        panel = tk.Frame(parent, bg=panel_bg, bd=1, relief=tk.SOLID, highlightbackground=border_color, highlightthickness=1)
        panel.bind("<Enter>", lambda e, k=model_id: self.show_model_description(k))
        panel.bind("<Leave>", self.clear_description)

        # --- Заголовок и значок RTX (с динамическим цветом) ---
        title_frame = tk.Frame(panel, bg=panel_bg)
        title_frame.pack(pady=(5, 2), padx=10, fill=tk.X)

        title_label = tk.Label(title_frame, text=model_name, font=("Segoe UI", 10, "bold"), bg=panel_bg, fg=title_color, anchor='w')
        title_label.pack(side=tk.LEFT, anchor='w')

        if requires_rtx30plus:
            gpu_meets_requirement = self.is_gpu_rtx30_or_40()
            icon_color = rtx_ok_color if gpu_meets_requirement else rtx_warning_color

            rtx_icon_label = tk.Label(
                title_frame,
                text="⚠️ RTX 30+",
                font=("Segoe UI", 7, "bold"),
                bg=panel_bg,
                fg=icon_color,
                anchor='w'
            )
            rtx_icon_label.pack(side=tk.LEFT, padx=(5, 0), anchor='w')

        # --- Информация о VRAM и GPU ---
        vram_text = f"VRAM: {model_data.get('min_vram', '?')}GB - {model_data.get('rec_vram', '?')}GB"
        gpu_req_text = f"GPU: {', '.join(supported_vendors)}" if supported_vendors else "GPU: Any"
        info_label = tk.Label(panel, text=f"{vram_text} | {gpu_req_text}", font=("Segoe UI", 8), bg=panel_bg, fg=text_color, anchor='w')
        info_label.pack(pady=(0, 5), padx=10, fill=tk.X)

        # --- Логика блокировки AMD и предупреждения ---
        allow_unsupported_gpu = os.environ.get("ALLOW_UNSUPPORTED_GPU", "0") == "1"
        is_amd_user = self.detected_gpu_vendor == "AMD"
        is_amd_supported = "AMD" in supported_vendors
        is_gpu_unsupported_amd = is_amd_user and not is_amd_supported
        show_warning_amd = allow_unsupported_gpu and is_gpu_unsupported_amd

        if show_warning_amd:
            warning_label_amd = tk.Label(panel, text="Может не работать на AMD!", font=("Segoe UI", 8, "bold"), bg=panel_bg, fg=warning_color_amd, anchor='w')
            warning_label_amd.pack(pady=(0, 5), padx=10, fill=tk.X)

        # --- Кнопка ---
        button_frame = tk.Frame(panel, bg=panel_bg)
        button_frame.pack(pady=(2, 6), padx=10, fill=tk.X)
        button_frame.columnconfigure(0, weight=1)

        is_installed = model_id in self.installed_models
        download_text = "Установлено" if is_installed else "Установить"

        can_interact = True
        if is_installed:
            can_interact = False
        elif is_gpu_unsupported_amd and not allow_unsupported_gpu:
            can_interact = False
            download_text = "Несовместимо с AMD"

        download_state_tk = tk.NORMAL if can_interact else tk.DISABLED

        download_button = tk.Button(
            button_frame, text=download_text,
            state=download_state_tk,
            bg=button_bg, fg=button_fg,
            activebackground=button_active_bg, activeforeground=button_fg,
            disabledforeground=button_disabled_fg,
            relief="flat", bd=0,
            font=("Segoe UI", 8, "bold"),
            padx=5, pady=5
        )
        self.download_buttons[model_id] = download_button

        if can_interact:
            download_button.configure(command=lambda mid=model_id, btn=download_button, mdata=model_data: self.confirm_and_start_download(mid, btn, mdata))

        download_button.grid(row=0, column=0, sticky="ew")

        return panel
    
    def is_gpu_rtx30_or_40(self):
        """
        Проверяет, является ли обнаруженная GPU NVIDIA RTX 30xx или 40xx,
        учитывая флаг окружения RTX_FORCE_UNSUPPORTED.
        """

        # Проверяем флаг принудительного неподдерживаемого статуса
        force_unsupported_str = os.environ.get("RTX_FORCE_UNSUPPORTED", "0")
        force_unsupported = force_unsupported_str.lower() in ['true', '1', 't', 'y', 'yes']

        if force_unsupported:
            print("INFO: RTX_FORCE_UNSUPPORTED=1 - Имитация неподходящей GPU для RTX 30+.")
            return False

        if self.detected_gpu_vendor != "NVIDIA" or not self.gpu_name:
            return False

        name_upper = self.gpu_name.upper()
        # Проверяем наличие "RTX" и цифр "30" или "40" в начале серии
        if "RTX" in name_upper:
            if any(f" {gen}" in name_upper or name_upper.endswith(gen) or f"-{gen}" in name_upper for gen in ["3050", "3060", "3070", "3080", "3090"]):
                return True
            if any(f" {gen}" in name_upper or name_upper.endswith(gen) or f"-{gen}" in name_upper for gen in ["4050", "4060", "4070", "4080", "4090"]):
                 return True
        return False

    def confirm_and_start_download(self, model_id, button_widget, model_data):
        """
        Показывает предупреждение для моделей rtx30plus, если GPU не соответствует,
        и затем запускает загрузку, если пользователь согласен.
        """
        requires_rtx30plus = model_data.get("rtx30plus", False)
        proceed_to_download = True # По умолчанию продолжаем

        if requires_rtx30plus:
            if not self.is_gpu_rtx30_or_40():
                gpu_info = self.gpu_name if self.gpu_name else "не определена"
                if self.detected_gpu_vendor and self.detected_gpu_vendor != "NVIDIA":
                    gpu_info = f"{self.detected_gpu_vendor} GPU"

                model_name = model_data.get("name", model_id)
                title = f"Предупреждение для модели '{model_name}'"
                message = (
                    f"Эта модель ('{model_name}') оптимизирована для видеокарт NVIDIA RTX 30xx/40xx.\n\n"
                    f"Ваша видеокарта ({gpu_info}) может не обеспечить достаточной производительности, "
                    "что может привести к медленной работе или нестабильности.\n\n"
                    "Продолжить установку?"
                )
                # Спрашиваем пользователя
                proceed_to_download = tkinter.messagebox.askokcancel(title, message, icon='warning', parent=self.master)

        if proceed_to_download:
            self.start_download(model_id, button_widget)

    def start_download(self, model_id, button_widget):
        if button_widget and button_widget.winfo_exists():
            # Используем состояние tk.DISABLED
            button_widget.config(text="Загрузка...", state=tk.DISABLED)

        if self.local_voice and hasattr(self.local_voice, 'download_model'):
            def install_thread_func():
                success = False
                try:
                    success = self.local_voice.download_model(model_id)
                except Exception as e:
                    print(f"Ошибка в потоке загрузки для {model_id}: {e}")
                finally:
                    if self.master.winfo_exists():
                        self.master.after(0, lambda: self.handle_download_result(success, model_id))

            install_thread = threading.Thread(target=install_thread_func, daemon=True)
            install_thread.start()
        else:
            print("Внимание: LocalVoice не доступен, используется имитация установки")
            self.master.after(2000 + hash(model_id)%1000, lambda: self.handle_download_result(True, model_id))

    def handle_download_result(self, success, model_id):
        button_widget = self.download_buttons.get(model_id)
        if success:
            self.installed_models.add(model_id)
            print(f"Модель {model_id} установлена. Перезагрузка и адаптация настроек...")
            self.load_settings()
            print("Настройки перезагружены.")

            # Обновляем кнопку уже после перезагрузки настроек
            button_widget = self.download_buttons.get(model_id)
            if button_widget and button_widget.winfo_exists():
                button_widget.config(text="Установлено", state=tk.DISABLED)

            self.display_installed_models_settings()
            self.save_installed_models_list() 

            if self.on_save_callback:
                 callback_data = {
                     "installed_models": list(self.installed_models),
                     "models_data": self.local_voice_models 
                 }
                 self.on_save_callback(callback_data)
            print(f"Обработка установки {model_id} завершена.")
        else:
            print(f"Ошибка установки модели {model_id}.")
            if button_widget and button_widget.winfo_exists():
                # Используем tk.NORMAL для состояния кнопки при ошибке
                button_widget.config(text="Ошибка", state=tk.NORMAL)

    def save_installed_models_list(self):
        try:
            with open(self.installed_models_file, "w", encoding="utf-8") as f:
                for model_id in sorted(list(self.installed_models)):
                    f.write(f"{model_id}\n")
        except Exception as e:
            print(f"Ошибка сохранения списка установленных моделей в {self.installed_models_file}: {e}")

    def show_setting_description(self, key):
        if self.description_label_widget and self.description_label_widget.winfo_exists():
            description = self.setting_descriptions.get(key, "")
            self.description_label_widget.config(text=description if description else default_description_text)

    def show_model_description(self, key):
        if self.description_label_widget and self.description_label_widget.winfo_exists():
            description = self.model_descriptions.get(key, "")
            self.description_label_widget.config(text=description if description else default_description_text)

    def clear_description(self, event=None):
        if self.description_label_widget and self.description_label_widget.winfo_exists():
            self.description_label_widget.config(text=default_description_text)

    def _update_scrollregion(self, canvas, scrollbar_widget):
        # Общий метод для обновления области прокрутки и показа/скрытия скроллбара
        if not canvas or not canvas.winfo_exists(): return
        canvas.update_idletasks()
        bbox = canvas.bbox("all")
        if bbox:
            scroll_region = (bbox[0], bbox[1], bbox[2], bbox[3])
            canvas.configure(scrollregion=scroll_region)
            scroll_height = scroll_region[3] - scroll_region[1]
            canvas_height = canvas.winfo_height()

            if scrollbar_widget and scrollbar_widget.winfo_exists():
                if scroll_height > canvas_height:
                    if not scrollbar_widget.winfo_ismapped():
                        # Пакуем относительно родительского фрейма канваса
                        scrollbar_widget.pack(side=tk.RIGHT, fill=tk.Y, in_=canvas.master)
                else:
                    if scrollbar_widget.winfo_ismapped():
                        scrollbar_widget.pack_forget()
                        canvas.yview_moveto(0)

    def _update_settings_scrollregion(self, event=None):
        # Находим скроллбар в родительском фрейме канваса настроек
        scrollbar = next((w for w in self.settings_canvas.master.winfo_children() if isinstance(w, ttk.Scrollbar)), None)
        self._update_scrollregion(self.settings_canvas, scrollbar)

    def _update_models_scrollregion(self, event=None):
        # Находим скроллбар в родительском фрейме канваса моделей
        scrollbar = next((w for w in self.models_canvas.master.winfo_children() if isinstance(w, ttk.Scrollbar)), None)
        self._update_scrollregion(self.models_canvas, scrollbar)

    def save_and_continue(self):
        self.save_settings()

    def save_and_quit(self):
        self.save_settings()
        if self.master:
            self.master.destroy()

    def run(self):
        self.master.mainloop()
