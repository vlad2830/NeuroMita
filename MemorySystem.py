import json
import os
import datetime


class MemorySystem:
    def __init__(self, character_name):

        self.history_dir = f"Histories\\{character_name}"
        os.makedirs(self.history_dir, exist_ok=True)

        self.filename = os.path.join(self.history_dir, f"{character_name}_memories.json")
        self.memories = []
        self.last_memory_number = 1
        if os.path.exists(self.filename):
            self.load_memories()

    def load_memories(self):
        with open(self.filename, 'r', encoding='utf-8') as file:
            self.memories = json.load(file)
            self.last_memory_number = len(self.memories)

    def save_memories(self):
        with open(self.filename, 'w', encoding='utf-8') as file:
            json.dump(self.memories, file, ensure_ascii=False, indent=4)

    def add_memory(self, content, date=datetime.datetime.now().strftime("%d.%m.%Y_%H.%M"), priority="Normal"):
        memory = {
            "N": self.last_memory_number,
            "date": date,
            "priority": priority,
            "content": content
        }
        self.last_memory_number += 1
        self.memories.append(memory)
        self.save_memories()

    def update_memory(self, number, content, priority=None):
        for memory in self.memories:
            if memory["N"] == number:
                if content is not None:
                    memory["date"] = datetime.datetime.now().strftime("%d.%m.%Y_%H.%M")
                    if priority:
                        memory["priority"] = priority
                    memory["content"] = content
                self.save_memories()
                return True
        return False

    def delete_memory(self, number):
        self.memories = [memory for memory in self.memories if memory["N"] != number]
        self.save_memories()

    def clear_memories(self):
        self.memories = []
        self.save_memories()
        self.last_memory_number = 1

    def get_memories_formatted(self):
        formatted_memories = []
        for memory in self.memories:
            formatted_memories.append(
                f"N:{memory['N']}, Date {memory['date']}, Priority: {memory['priority']}: {memory['content']}"
            )
        return "LongMemory< " + "\n".join(formatted_memories) + " >EndLongMemory"

# Пример использования
#memory_system = MemorySystem()
#memory_system.add_memory(1, "2023-10-01", "Высокая", "Первое воспоминание")
#memory_system.add_memory(2, "2023-10-02", "Средняя", "Второе воспоминание")
#print(memory_system.get_memories_formatted())
