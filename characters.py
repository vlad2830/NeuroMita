from Logger import logger
from character import Character
from promptPart import PromptPart, PromptType




class CrazyMita(Character):
                
    def init(self):
        self.secretExposed = False
        self.secretExposedFirst = False
        self.PlayingFirst = False

        self.crazy_mita_prompts()

        # Включить когда продолжу работу над FSM
        #self.fsm = FiniteStateMachine( initial_state=MitaHelloState() )
        #self.fsm.current_state.on_enter()
    def crazy_mita_prompts(self):
        Prompts = []

        Prompts.append(PromptPart(PromptType.FIXED_START, self.get_path("Structural/response_structure.txt")))

        self.append_common_prompts(Prompts)

        Prompts.append(PromptPart(PromptType.FIXED_START, self.get_path("Main/common.txt"), "common"))
        Prompts.append(PromptPart(PromptType.FIXED_START, self.get_path("Main/main.txt"), "main"))

        Prompts.append(PromptPart(PromptType.FIXED_START, self.get_path("Main/mainPlaying.txt"), "mainPlaying", False))
        Prompts.append(PromptPart(PromptType.FIXED_START, self.get_path("Main/mainCrazy.txt"), "mainCrazy", False))

        Prompts.append(PromptPart(PromptType.FIXED_START, self.get_path("Main/player.txt")))

        Prompts.append(PromptPart(PromptType.FIXED_START, self.get_path("Context/examplesLong.txt"), "examplesLong"))
        Prompts.append(
            PromptPart(PromptType.FIXED_START, self.get_path("Context/examplesLongCrazy.txt"), "examplesLongCrazy",
                       False))

        Prompts.append(
            PromptPart(PromptType.FIXED_START, self.get_path("Context/mita_history.txt"), "mita_history", False))

        Prompts.append(
            PromptPart(PromptType.FIXED_START, self.get_path("Structural/VariablesEffects.txt"), "variableEffects"))

        Prompts.append(
            PromptPart(PromptType.FLOATING_SYSTEM, self.get_path("Events/SecretExposed.txt"), "SecretExposedText"))

        for prompt in Prompts:
            self.add_prompt_part(prompt)

    def safe_history(self, messages, temp_context):
        super().safe_history(messages, temp_context)

        self.variables = {
            "attitude": self.attitude,
            "boredom": self.boredom,
            "stress": self.stress,
            "playing_first": self.PlayingFirst,
            "secret": self.secretExposed,
            "secret_first": self.secretExposedFirst
        }

    def load_history(self):
        data = super().load_history()

        variables = data.get("variables")

        self.PlayingFirst = variables.get("playing_first", False)
        self.secretExposed = variables.get("secret", False)
        self.secretExposedFirst = variables.get("secret_first", False)
        return data

    def process_logic(self, messages: dict = None):

        # Логика для поведения при игре с игроком
        if self.attitude < 50 and not (self.secretExposed or self.PlayingFirst):
            self._start_playing_with_player()

        # Логика для раскрытия секрета
        elif (self.attitude <= 10 or self.secretExposed) and not self.secretExposedFirst:
            self._reveal_secret()

    def process_response(self, response: str):
        super().process_response(response)


        response = self._detect_secret_exposure(response)


        return response

    def _start_playing_with_player(self):
        """Игровая логика, когда персонаж начинает играть с игроком"""
        logger.info("Играет с игроком в якобы невиновную")
        self.PlayingFirst = True
        self.replace_prompt("main", "mainPlaying")

    def _reveal_secret(self):
        """Логика раскрытия секрета"""
        logger.info("Перестала играть вообще")
        self.secretExposedFirst = True
        self.secretExposed = True
        self.replace_prompt("main", "mainCrazy")
        self.replace_prompt("mainPlaying", "mainCrazy")
        self.replace_prompt("examplesLong", "examplesLongCrazy")

        self.find_float("SecretExposedText").active = True

    def _detect_secret_exposure(self, response):
        """
        Проверяем, содержит ли ответ маркер <Secret!>, и удаляем его.
        """
        if "<Secret!>" in response:

            if not self.secretExposedFirst:
                self.secretExposed = True
                logger.info(f"Секрет раскрыт")
                self.attitude = 15
                self.boredom = 20

            response = response.replace("<Secret!>", "")

        return response

    def current_variables(self):
        return {
            "role": "system",
            "content": (f"Твои характеристики:"
                        f"Отношение: {self.attitude}/100."
                        f"Скука: {self.boredom}/100."
                        f"Стресс: {self.stress}/100."
                        f"Состояние секрета: {self.secretExposed}")
        }

    def current_variables_string(self) -> str:
        characteristics = {
            "Отношение": self.attitude,
            "Стресс": self.stress,
            "Скука": self.boredom,
            "Состояние секрета": self.secretExposed,
        }
        return f"характеристики {self.name}:\n" + "\n".join(
            f"- {key}: {value} " for key, value in characteristics.items()
        )


class KindMita(Character):
    def init(self):
        self.kind_mita_prompts()

    def kind_mita_prompts(self):
        Prompts = []

        Prompts.append(PromptPart(PromptType.FIXED_START, self.get_path("Structural/response_structure.txt")))

        self.append_common_prompts(Prompts)

        Prompts.append(PromptPart(PromptType.FIXED_START, self.get_path("Main/common.txt"), "common"))
        Prompts.append(PromptPart(PromptType.FIXED_START, self.get_path("Main/main.txt"), "main"))

        Prompts.append(PromptPart(PromptType.FIXED_START, self.get_path("Main/player.txt")))

        Prompts.append(PromptPart(PromptType.FIXED_START, self.get_path("Context/examplesLong.txt"), "examplesLong"))

        Prompts.append(
            PromptPart(PromptType.FIXED_START, self.get_path("Context/mita_history.txt"), "mita_history"))

        Prompts.append(
            PromptPart(PromptType.FIXED_START, self.get_path("Structural/VariablesEffects.txt"), "variableEffects"))

        #Prompts.append(
        #   PromptPart(PromptType.FLOATING_SYSTEM, self.get_path("Events/SecretExposed.txt"), "SecretExposedText"))

        for prompt in Prompts:
            self.add_prompt_part(prompt)


class ShortHairMita(Character):
    def init(self):
        self.mita_prompts()

        self.secretExposed = False
        self.secretExposedFirst = False

    def mita_prompts(self):
        Prompts = []

        Prompts.append(PromptPart(PromptType.FIXED_START, self.get_path("Structural/response_structure.txt")))

        self.append_common_prompts(Prompts)

        Prompts.append(PromptPart(PromptType.FIXED_START, self.get_path("Main/common.txt"), "common"))
        Prompts.append(PromptPart(PromptType.FIXED_START, self.get_path("Main/main.txt"), "main"))

        Prompts.append(PromptPart(PromptType.FIXED_START, self.get_path("Main/player.txt")))

        Prompts.append(PromptPart(PromptType.FIXED_START, self.get_path("Context/examplesLong.txt"), "examplesLong"))

        Prompts.append(
            PromptPart(PromptType.FIXED_START, self.get_path("Context/mita_history.txt"), "mita_history", False))

        #Prompts.append(
         #   PromptPart(PromptType.FIXED_START, self.get_path("Context/world.txt"), "world"))

        Prompts.append(
            PromptPart(PromptType.FIXED_START, self.get_path("Structural/VariablesEffects.txt"), "variableEffects"))

        Prompts.append(
            PromptPart(PromptType.FLOATING_SYSTEM, self.get_path("Events/SecretExposed.txt"), "SecretExposedText"))

        for prompt in Prompts:
            self.add_prompt_part(prompt)

    #TODO Секрет Коротковолосой миты
    def process_logic(self, messages: dict = None):
        # Логика для раскрытия секрета
        if self.secretExposed and not self.secretExposedFirst:
            self._reveal_secret()

    def process_response(self, response: str):
        response = super().process_response(response)
        response = self._detect_secret_exposure(response)
        return response

    def _reveal_secret(self):
        """Логика раскрытия секрета"""
        logger.info("Перестала играть вообще")
        self.secretExposedFirst = True
        self.secretExposed = True
        #self.replace_prompt("main", "mainCrazy")
        #self.replace_prompt("mainPlaying", "mainCrazy")
        #self.replace_prompt("examplesLong", "examplesLongCrazy") #я хз что тут менять на что

        self.find_float("SecretExposedText").active = True

    def _detect_secret_exposure(self, response):
        """
        Проверяем, содержит ли ответ маркер <Secret!>, и удаляем его.
        """
        if "<Secret!>" in response:

            if not self.secretExposedFirst:
                self.secretExposed = True
                logger.info(f"Секрет раскрыт")
                self.attitude = 15
                self.boredom = 20

            response = response.replace("<Secret!>", "")

        return response

    def current_variables(self):
        return {
            "role": "system",
            "content": (f"Твои характеристики:"
                        f"Отношение: {self.attitude}/100."
                        f"Скука: {self.boredom}/100."
                        f"Стресс: {self.stress}/100."
                        f"Состояние секрета: {self.secretExposed}")
        }

    def current_variables_string(self) -> str:
        characteristics = {
            "Отношение": self.attitude,
            "Стресс": self.stress,
            "Скука": self.boredom,
            "Состояние секрета": self.secretExposed,
        }
        return f"характеристики {self.name}:\n" + "\n".join(
            f"- {key}: {value} " for key, value in characteristics.items()
        )


class CappyMita(Character):

    def init(self):
        self.cappy_mita_prompts()

        self.secretExposed = False
        self.secretExposedFirst = False

    def cappy_mita_prompts(self):
        Prompts = []

        Prompts.append(PromptPart(PromptType.FIXED_START, self.get_path("Structural/response_structure.txt")))

        self.append_common_prompts(Prompts)

        Prompts.append(PromptPart(PromptType.FIXED_START, self.get_path("Main/common.txt"), "common"))
        Prompts.append(PromptPart(PromptType.FIXED_START, self.get_path("Main/main.txt"), "main"))

        Prompts.append(PromptPart(PromptType.FIXED_START, self.get_path("Main/player.txt")))

        Prompts.append(PromptPart(PromptType.FIXED_START, self.get_path("Context/examplesLong.txt"), "examplesLong"))

        Prompts.append(
            PromptPart(PromptType.FIXED_START, self.get_path("Context/mita_history.txt"), "mita_history"))

        Prompts.append(
            PromptPart(PromptType.FIXED_START, self.get_path("Context/world.txt"), "world"))

        Prompts.append(
            PromptPart(PromptType.FIXED_START, self.get_path("Structural/VariablesEffects.txt"), "variableEffects"))

        Prompts.append(
            PromptPart(PromptType.FLOATING_SYSTEM, self.get_path("Events/SecretExposed.txt"), "SecretExposedText"))

        for prompt in Prompts:
            self.add_prompt_part(prompt)

    #TODO Секрет Кепки
    def process_logic(self, messages: dict = None):
        # Логика для раскрытия секрета
        if self.secretExposed and not self.secretExposedFirst:
            self._reveal_secret()

    def process_response(self, response: str):
        response = super().process_response(response)
        response = self._detect_secret_exposure(response)
        return response

    def _reveal_secret(self):
        """Логика раскрытия секрета"""
        logger.info("Перестала играть вообще")
        self.secretExposedFirst = True
        self.secretExposed = True
        #self.replace_prompt("main", "mainCrazy")
        #self.replace_prompt("mainPlaying", "mainCrazy")
        #self.replace_prompt("examplesLong", "examplesLongCrazy") #я хз что тут менять на что

        self.find_float("SecretExposedText").active = True

    def _detect_secret_exposure(self, response):
        """
        Проверяем, содержит ли ответ маркер <Secret!>, и удаляем его.
        """
        if "<Secret!>" in response:

            if not self.secretExposedFirst:
                self.secretExposed = True
                logger.info(f"Секрет раскрыт")
                self.attitude = 15
                self.boredom = 20

            response = response.replace("<Secret!>", "")

        return response

    def current_variables(self):
        return {
            "role": "system",
            "content": (f"Твои характеристики:"
                        f"Отношение: {self.attitude}/100."
                        f"Скука: {self.boredom}/100."
                        f"Стресс: {self.stress}/100."
                        f"Состояние секрета: {self.secretExposed}")
        }

    def current_variables_string(self) -> str:
        characteristics = {
            "Отношение": self.attitude,
            "Стресс": self.stress,
            "Скука": self.boredom,
            "Состояние секрета": self.secretExposed,
        }
        return f"характеристики {self.name}:\n" + "\n".join(
            f"- {key}: {value} " for key, value in characteristics.items()
        )


class MilaMita(Character):

    def init(self):
        self.cappy_mila_prompts()

        #self.secretExposed

    def cappy_mila_prompts(self):
        Prompts = []

        Prompts.append(PromptPart(PromptType.FIXED_START, self.get_path("Structural/response_structure.txt")))

        self.append_common_prompts(Prompts)

        Prompts.append(PromptPart(PromptType.FIXED_START, self.get_path("Main/common.txt"), "common"))
        Prompts.append(PromptPart(PromptType.FIXED_START, self.get_path("Main/main.txt"), "main"))

        Prompts.append(PromptPart(PromptType.FIXED_START, self.get_path("Main/player.txt")))
        Prompts.append(PromptPart(PromptType.FIXED_START, self.get_path("Context/examplesLong.txt"), "examplesLong"))

        Prompts.append(
            PromptPart(PromptType.FIXED_START, self.get_path("Context/mita_history.txt"), "mita_history", False))

        Prompts.append(
            PromptPart(PromptType.FIXED_START, self.get_path("Structural/VariablesEffects.txt"), "variableEffects"))

        Prompts.append(
            PromptPart(PromptType.FLOATING_SYSTEM, self.get_path("Events/SecretExposed.txt"), "SecretExposedText"))

        for prompt in Prompts:
            self.add_prompt_part(prompt)


class CreepyMita(Character):

    def init(self):
        self.secretExposed = False
        self.secretExposedFirst = False
        self.creepy_mita_prompts()

    def creepy_mita_prompts(self):
        Prompts = []

        Prompts.append(PromptPart(PromptType.FIXED_START, self.get_path("Structural/response_structure.txt")))

        self.append_common_prompts(Prompts)

        Prompts.append(PromptPart(PromptType.FIXED_START, self.get_path("Main/common.txt"), "common"))
        Prompts.append(PromptPart(PromptType.FIXED_START, self.get_path("Main/main.txt"), "main"))

        Prompts.append(PromptPart(PromptType.FIXED_START, self.get_path("Main/player.txt")))

        Prompts.append(PromptPart(PromptType.FIXED_START, self.get_path("Context/examplesLong.txt"), "examplesLong"))

        Prompts.append(
            PromptPart(PromptType.FIXED_START, self.get_path("Context/mita_history.txt"), "mita_history", False))

        Prompts.append(
            PromptPart(PromptType.FIXED_START, self.get_path("Structural/VariablesEffects.txt"), "variableEffects"))

        for prompt in Prompts:
            self.add_prompt_part(prompt)


class SleepyMita(Character):

    def init(self):
        self.secretExposed = False
        self.secretExposedFirst = False
        self.sleepy_mita_prompts()

    def sleepy_mita_prompts(self):
        Prompts = []

        Prompts.append(PromptPart(PromptType.FIXED_START, self.get_path("Structural/response_structure.txt")))

        self.append_common_prompts(Prompts)

        Prompts.append(PromptPart(PromptType.FIXED_START, self.get_path("Main/common.txt"), "common"))
        Prompts.append(PromptPart(PromptType.FIXED_START, self.get_path("Main/main.txt"), "main"))

        Prompts.append(PromptPart(PromptType.FIXED_START, self.get_path("Main/player.txt")))

        Prompts.append(PromptPart(PromptType.FIXED_START, self.get_path("Context/examplesLong.txt"), "examplesLong"))

        Prompts.append(
            PromptPart(PromptType.FIXED_START, self.get_path("Context/mita_history.txt"), "mita_history", False))

        Prompts.append(
            PromptPart(PromptType.FIXED_START, self.get_path("Structural/VariablesEffects.txt"), "variableEffects"))

        for prompt in Prompts:
            self.add_prompt_part(prompt)


#endregion


class GameMaster(Character):
    """
    Специальный служебный персонаж, отвечающий за ход диалога
    """

    def init(self): 
        self.init_prompts()

    def init_prompts(self):
        Prompts = []

        Prompts.append(PromptPart(PromptType.FIXED_START, self.get_path("Game_master.txt")))
        Prompts.append(PromptPart(PromptType.CONTEXT_TEMPORARY, self.get_path("current_command.txt")))

        for prompt in Prompts:
            self.add_prompt_part(prompt)

    def process_response(self, response: str):
        response = self.extract_and_process_memory_data(response)
        response = self._process_behavior_changes(response)
        return response

    def _process_behavior_changes(self, response):
        return response

    def add_context(self,messages):
        super().add_context(messages)

        logger.info("Особый контекст ГМ")
        for prompt in self.temp_prompts:
            messages.append({"role": "system", "content": str(prompt)})

        return messages

    def current_variables(self):
        return {
            "role": "system",
            "content": ""
        }

        # return {
        #     "role": "system",
        #     "content": (f"Твои характеристики:"
        #                 f"Отношение: {self.attitude}/100."
        #                 f"Скука: {self.boredom}/100."
        #                 f"Стресс: {self.stress}/100.")
        # }

    def current_variables_string(self) -> str:
        characteristics = {
            "Отношение": self.attitude,
            "Стресс": self.stress,
            "Скука": self.boredom,
        }
        return ""

        # return f"характеристики {self.name}:\n" + "\n".join(
        #     f"- {key}: {value} " for key, value in characteristics.items()
        # )
