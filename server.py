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

        self.gui.ConnectedToGame = True

    def handle_connection(self):
        """Обрабатывает одно подключение."""
        if not self.server_socket:
            raise RuntimeError("Сервер не запущен. Вызовите start() перед handle_connection().")
        try:
            #print("Жду получения от клиента игры")
            # Ожидание подключения
            self.client_socket, addr = self.server_socket.accept()
            #print(f"Подключен {addr}")

            # Получение сообщения от клиента
            received_text = self.client_socket.recv(4086).decode('utf-8')
            #print("Получил")
            # Разделяем текст и ссылку по "|||"
            character, message, system_message, system_info, self.chat_model.distance, self.chat_model.roomPlayer, self.chat_model.roomMita, self.chat_model.nearObjects, self.chat_model.actualInfo = received_text.split(
                "|||")

            self.chat_model.current_character_to_change = character

            if system_info != "-":
                print("Добавил систем инфо " + system_info)
                self.chat_model.add_temporary_system_info(system_info)

            response = ""
            if message == "waiting":
                if system_message != "-":
                    print("Получено system_message")
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

            # Ждать ли на той стороне файла озвучки
            if self.gui.silero_connected.get():
                silero = "1"
            else:
                silero = "0"

            if not character:
                character = "Mita"

            message = f"{character}|||{response}|||{silero}|||{self.gui.patch_to_sound_file}"
            self.gui.patch_to_sound_file = ""

            # Отправляем сообщение через сокет
            #print("Отправляю обратно в игру")
            self.client_socket.send(message.encode('utf-8'))
            #print("Получил")
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
            response = self.chat_model.generate_response(input_text, system_input_text)
            #while self.chat_model.repeatResponse and counter<3:
            #   response += self.chat_model.generate_response("", "")
            # counter+=1

            if input_text != "":
                self.gui.insertDialog(input_text, response)
        except Exception as e:
            print(f"Ошибка генерации ответа: {e}")
            response = "Произошла ошибка при обработке вашего сообщения."
        return response

    def send_message_to_server(self, message):
        self.MessagesToSay.append(message)

    def stop(self):
        """Закрывает сервер."""
        if self.server_socket:
            self.server_socket.close()
            print("Сервер остановлен.")
            self.gui.ConnectedToGame = False
