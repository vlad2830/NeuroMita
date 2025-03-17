import json
import socket
import datetime


class ChatServer:
    def __init__(self, gui, chat_model, host='127.0.0.1', port=12345):
        self.host = host
        self.port = port
        self.gui = gui
        self.server_socket = None
        self.client_socket = None
        self.passive_client_socket = None
        self.passive_server_socket = None
        self.chat_model = chat_model
        self.MessagesToSay = list()

    def start(self):
        """Инициализирует и запускает сервер."""
        self.server_socket = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
        self.server_socket.bind((self.host, self.port))
        self.server_socket.listen(5)
        print(f"Сервер запущен на {self.host}:{self.port}")

    #TODO РЕФАКТОРЬ РЕФАКТОРЬ РЕФАКТОРЬ!
    def handle_connection(self):
        """Обрабатывает одно подключение."""
        if not self.server_socket:
            raise RuntimeError("Сервер не запущен. Вызовите start() перед handle_connection().")
        try:
            #print("Жду получения от клиента игры")
            # Ожидание подключения
            self.client_socket, addr = self.server_socket.accept()
            #print(f"Подключен {addr}")

            received_text = self.client_socket.recv(8192).decode("utf-8")

            # Логируем полученные данные
            #print(f"Получено: {received_text}")

            # Проверяем, корректно ли закрыт JSON (простая проверка)
            if not received_text.strip().endswith("}"):
                print("Ошибка: JSON оборван")
            else:
                try:
                    message_data = json.loads(received_text)
                    #print("JSON успешно разобран!")
                except json.JSONDecodeError as e:
                    print(f"Ошибка обработки JSON: {e}")

            message_id = message_data["id"]
            self.gui.id_sound = message_id

            message_type = message_data["type"]

            character = message_data["character"]
            message = message_data["input"]
            system_message = message_data["dataToSentSystem"]
            system_info = message_data["systemInfo"]

            # Вот это этого надо будет изьавиться
            self.chat_model.distance = float(message_data["distance"].replace(",", "."))
            self.chat_model.roomPlayer = int(message_data["roomPlayer"])
            self.chat_model.roomMita = int(message_data["roomMita"])
            self.chat_model.nearObjects = message_data["hierarchy"]
            #

            self.chat_model.actualInfo = message_data["currentInfo"]

            self.chat_model.current_character_to_change = character

            if system_info != "-":
                print("Добавил систем инфо " + system_info)
                self.chat_model.add_temporary_system_info(system_info)

            response = ""
            if message == "waiting":
                if system_message != "-":
                    print(f"Получено system_message {system_message}")
                    response = self.generate_response("", system_message)
                    self.gui.insertDialog("", response)
                elif len(self.MessagesToSay) > 0:
                    response = self.MessagesToSay.pop(0)
            elif message == "boring":
                date_now = datetime.datetime.now().replace(microsecond=0)
                response = self.generate_response("",
                                                  f"Время {date_now}, Игрок долго молчит( Ты можешь что-то сказать или предпринять")
                self.gui.insertDialog("", response)
                print("Отправлено Мите на озвучку: " + response)
            else:
                print("Получено message")
                # Если игрок отправил внутри игры, message его
                response = self.generate_response(message, "")
                #self.gui.insertDialog(message,response)
                print("Отправлено Мите на озвучку: " + response)

            if not character:
                character = "Mita"

            transmitted_to_game = False
            if self.gui.user_input:
                transmitted_to_game = True

            message_data = {
                "id": int(message_id),
                "type": str(message_type),
                "character": str(character),
                "response": str(response),
                "silero": bool(self.gui.silero_connected and bool(self.gui.settings.get("SILERO_USE"))),
                "id_sound": int(1),
                "patch_to_sound_file": str(self.gui.patch_to_sound_file),
                "user_input": str(self.gui.user_input),

                # Простите, но я хотел за вечер затестить
                "GM_ON": bool(self.gui.settings.get("GM_ON")),
                "GM_READ": bool(self.gui.settings.get("GM_READ")),
                "GM_VOICE": bool(self.gui.settings.get("GM_VOICE"))

            }

            self.gui.patch_to_sound_file = ""

            if transmitted_to_game:
                self.gui.clear_user_input()

            # Отправляем JSON через сокет
            json_message = json.dumps(message_data)
            self.client_socket.send(json_message.encode("utf-8"))

            self.gui.ConnectedToGame = True
            return True
        except Exception as e:
            print(f"Ошибка обработки подключения: {e}")
            self.gui.ConnectedToGame = False
        finally:
            if self.client_socket:
                self.client_socket.close()
            return False

    def generate_response(self, input_text, system_input_text):
        """Генерирует текст с помощью модели."""
        try:

            self.gui.waiting_answer = True
            response = self.chat_model.generate_response(input_text, system_input_text)
            if input_text != "":
                self.gui.insertDialog(input_text, response)

        except Exception as e:
            print(f"Ошибка генерации ответа: {e}")
            response = "Произошла ошибка при обработке вашего сообщения."

        self.gui.waiting_answer = False
        return response

    def send_message_to_server(self, message):
        self.MessagesToSay.append(message)

    def stop(self):
        """Закрывает сервер."""
        if self.server_socket:
            self.server_socket.close()
            print("Сервер остановлен.")
            self.gui.ConnectedToGame = False
