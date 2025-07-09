import requests

# Test API
response = requests.post(
    "http://localhost:8000/chat",
    json={"question": "Tác giả của trò chơi là ai?"}
)

print("Response:", response.json())
