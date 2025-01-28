class PromptsManager:
    def __init__(self):
        self.common = self.load_text_from_file("Promts/Main/common.txt")
        self.main = self.load_text_from_file("Promts/Main/main.txt")
        self.player = self.load_text_from_file("Promts/Main/player.txt")
        self.mainPlaying = self.load_text_from_file("Promts/Main/mainPlaing.txt")
        self.mainCrazy = self.load_text_from_file("Promts/Main/mainCrazy.txt")
        self.examplesLong = self.load_text_from_file("Promts/Context/examplesLong.txt")
        self.examplesLongCrazy = self.load_text_from_file("Promts/Context/examplesLongCrazy.txt")
        self.world = self.load_text_from_file("Promts/Context/world.txt")
        self.mita_history = self.load_text_from_file("Promts/Context/mita_history.txt")
        self.variableEffects = self.load_text_from_file("Promts/Structural/VariablesEffects.txt")
        self.response_structure = self.load_text_from_file("Promts/Structural/response_structure.txt")
        self.SecretExposedText = self.load_text_from_file("Promts/Events/SecretExposed.txt")

    def load_text_from_file(self, file_path):
        with open(file_path, "r", encoding="utf-8") as file:
            return file.read()