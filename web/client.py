import httpx
import asyncio
import logging

logger = logging.getLogger(__name__)

class BaseClient:
    base_url: str = None

    @classmethod
    async def send_request(cls, endpoint: str, data: dict = None, method: str = "GET", headers: dict = None, params: dict = None, timeout: int = 10):
        url = f"{cls.base_url}/{endpoint}"
        async with httpx.AsyncClient() as client:
            try:
                response = await client.request(method, url, json=data, timeout=timeout, headers=headers, params=params)
                response.raise_for_status()
                return response
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
    base_url = "http://109.110.73.254:2020/api/v1"


async def main():
    response = await MikuTTSClient.send_request(method="GET", endpoint="get_edge", params={"text": "Привет, тестер!", 
                                                          "person": "CrazyMita",
                                                          "rate": "+20%",
                                                          "pitch": 10})
    print(response.content)
    with open("test.wav", "wb") as f:
        f.write(response.content)

if __name__ == "__main__":
    asyncio.run(main())