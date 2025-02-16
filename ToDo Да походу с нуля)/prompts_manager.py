class PromptsManager:
    def __init__(self):
        self.common = self.load_text_from_file("../Prompts/CrazyMitaPrompts/Main/common.txt")
        self.main = self.load_text_from_file("../Prompts/CrazyMitaPrompts/Main/main.txt")
        self.player = self.load_text_from_file("../Prompts/CrazyMitaPrompts/Main/player.txt")
        self.mainPlaying = self.load_text_from_file("../Prompts/CrazyMitaPrompts/Main/mainPlaing.txt")
        self.mainCrazy = self.load_text_from_file("../Prompts/CrazyMitaPrompts/Main/mainCrazy.txt")
        self.examplesLong = self.load_text_from_file("../Prompts/CrazyMitaPrompts/Context/examplesLong.txt")
        self.examplesLongCrazy = self.load_text_from_file("../Prompts/CrazyMitaPrompts/Context/examplesLongCrazy.txt")
        self.world = self.load_text_from_file("../Prompts/CrazyMitaPrompts/NotUsedNow/world.txt")
        self.mita_history = self.load_text_from_file("../Prompts/CrazyMitaPrompts/Context/mita_history.txt")
        self.variableEffects = self.load_text_from_file("../Prompts/CrazyMitaPrompts/Structural/VariablesEffects.txt")
        self.response_structure = self.load_text_from_file(
            "../Prompts/CrazyMitaPrompts/Structural/response_structure.txt")
        self.SecretExposedText = self.load_text_from_file("../Prompts/CrazyMitaPrompts/Events/SecretExposed.txt")

    def load_text_from_file(self, file_path):
        with open(file_path, "r", encoding="utf-8") as file:
            return file.read()