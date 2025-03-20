import time
import httpx
import asyncio
import logging

logger = logging.getLogger(__name__)

class BaseClient:
    base_url: str = None

    @classmethod
    async def send_request(cls, endpoint: str, port: int = 2020, data: dict = None, method: str = "GET", headers: dict = None, params: dict = None, timeout: int = 10):
        url = f"{cls.base_url}:{port}/api/v1/{endpoint}"
        async with httpx.AsyncClient() as client:
            try:
                start_time = time.time()
                response = await client.request(method, url, data=data, timeout=timeout, headers=headers, params=params)
                end_time = time.time()
                response.raise_for_status()
                return response, end_time - start_time
            except httpx.HTTPStatusError as e:
                logger.error(f"HTTP Error: {e.response.text}")
                raise e
            except httpx.TimeoutException as e:
                logger.error(f"Таймаут запроса: {e}")
                raise e
            except Exception as e:
                logger.error(f"Error: {e}")
                raise e

class MikuTTSClient(BaseClient):
    base_url = "http://109.110.73.254"


async def main():
    response, time_taken = await MikuTTSClient.send_request(method="GET", endpoint="get_edge", params={"text": "Привет, тестер!", 
                                                          "person": "CrazyMita",
                                                          "rate": "+20%",
                                                          "pitch": 10})
    print(f"Время генерации озвучки: {time_taken} секунд")
    print(response.content)
    with open("test.wav", "wb") as f:
        f.write(response.content)

if __name__ == "__main__":
    asyncio.run(main())