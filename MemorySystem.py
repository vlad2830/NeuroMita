import json
import os


class MemorySystem:
    def __init__(self, filename="memories.json"):
        self.filename = filename
        self.memories = []
        if os.path.exists(self.filename):
            self.load_memories()

    def load_memories(self):
        with open(self.filename, 'r', encoding='utf-8') as file:
            self.memories = json.load(file)

    def save_memories(self):
        with open(self.filename, 'w', encoding='utf-8') as file:
            json.dump(self.memories, file, ensure_ascii=False, indent=4)

    def add_memory(self, number, date, priority, content):
        memory = {
            "number": number,
            "date": date,
            "priority": priority,
            "content": content
        }
        self.memories.append(memory)
        self.save_memories()

    def update_memory(self, number, date=None, priority=None, content=None):
        for memory in self.memories:
            if memory["number"] == number:
                if date is not None:
                    memory["date"] = date
                if priority is not None:
                    memory["priority"] = priority
                if content is not None:
                    memory["content"] = content
                self.save_memories()
                return True
        return False

    def delete_memory(self, number):
        self.memories = [memory for memory in self.memories if memory["number"] != number]
        self.save_memories()

    def clear_memories(self):
        self.memories = []
        self.save_memories()

    def get_memories_formatted(self):
        formatted_memories = []
        for memory in self.memories:
            formatted_memories.append(
                f"Номер: {memory['number']}, Дата: {memory['date']}, Важность: {memory['priority']}, Контент: {memory['content']}"
            )
        return "\n".join(formatted_memories)


# Пример использования
#memory_system = MemorySystem()
#memory_system.add_memory(1, "2023-10-01", "Высокая", "Первое воспоминание")
#memory_system.add_memory(2, "2023-10-02", "Средняя", "Второе воспоминание")
#print(memory_system.get_memories_formatted())
