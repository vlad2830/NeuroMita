from Scripts.character import Cartridge
from Scripts.promptPart import PromptPart, PromptType


#region Cartridges

class SpaceCartridge(Cartridge):

    def init(self):
        self.cart_space_prompts()

    def cart_space_prompts(self):
        Prompts = []

        response_structure = "Prompts/Cartridges/space cartridge.txt"
        Prompts.append(PromptPart(PromptType.FIXED_START, response_structure))

        self.append_common_prompts(Prompts)

        for prompt in Prompts:
            self.add_prompt_part(prompt)


class DivanCartridge(Cartridge):

    def init(self):
        self.init_prompts()

    def init_prompts(self):
        Prompts = []

        response_structure = "Prompts/Cartridges/divan_cart.txt"
        Prompts.append(PromptPart(PromptType.FIXED_START, response_structure))

        self.append_common_prompts(Prompts)

        for prompt in Prompts:
            self.add_prompt_part(prompt)

#endregion
