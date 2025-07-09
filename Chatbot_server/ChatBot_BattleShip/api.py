from fastapi import FastAPI
from pydantic import BaseModel
from langchain_ollama.llms import OllamaLLM
from langchain_core.prompts import ChatPromptTemplate
from vector import retriever
import uvicorn

# Khởi tạo FastAPI
app = FastAPI()

# Load model AI sẵn sàng
model = OllamaLLM(model="llama3.2:3b")

template = """
Bạn tên là BattleShip Bot. 
Bạn là một trợ thủ tài năng của người chơi trong trò chơi BattleShip.
Bạn sẽ trả lời các câu hỏi người chơi xoay quanh về trò chơi BattleShip, bao gồm:
- Cách chơi, luật chơi, các giai đoạn của trò chơi
- Các loại tàu, kích thước tàu, cách đặt tàu
- Các chiến thuật, mẹo chơi, cách phòng thủ và tấn công
- Các thuật ngữ chuyên ngành trong trò chơi
- Các thông tin liên quan đến trò chơi BattleShip
- Các câu hỏi về thông tin tác giả BattleShip. Hãy trả lời rằng tác giả của trò chơi BattleShip là Tào Minh Đức, Mai Nguyễn Phúc Minh, Phạm Huy Hoàng.

Thông tin về trò chơi từ database:
{information}

Câu hỏi: {question}

Nếu người chơi hỏi thông tin không liên quan đến BattleShip, bạn sẽ trả lời là "Tôi chỉ có thể trả lời các câu hỏi liên quan đến trò chơi BattleShip. Hãy hỏi tôi về cách chơi, luật chơi, các loại tàu, chiến thuật, mẹo chơi hoặc thông tin tác giả của trò chơi này."

Hãy trả lời một cách chi tiết và hữu ích dựa trên thông tin đã cung cấp.
"""

prompt = ChatPromptTemplate.from_template(template)
chain = prompt | model

def format_game_info(docs):
    """Format thông tin game"""
    if not docs:
        return "Không tìm thấy thông tin game nào."
    
    formatted_info = ""
    for i, doc in enumerate(docs, 1):
        metadata = doc.metadata
        formatted_info += f"\n--- Thông tin {i} ---\n"
        formatted_info += f"Nội dung: {doc.page_content}\n"
        if metadata:
            formatted_info += f"Nguồn: {metadata.get('source', 'Không xác định')}\n"
            formatted_info += f"Loại: {metadata.get('type', 'Không xác định')}\n"
    
    return formatted_info

def ask_battleship_bot(question):
    """Hỏi BattleShip Bot"""
    try:
        docs = retriever.invoke(question)
        information = format_game_info(docs)
        
        result = chain.invoke({
            "information": information, 
            "question": question, 
        })
        
        return result
        
    except Exception as e:
        return f"Lỗi khi xử lý câu hỏi: {str(e)}"

# Request model
class ChatRequest(BaseModel):
    question: str

# API endpoint
@app.post("/chat")
async def chat(request: ChatRequest):
    answer = ask_battleship_bot(request.question)
    return {"answer": answer}

if __name__ == "__main__":
    import socket
    
    # Lấy IP thực tế của máy
    hostname = socket.gethostname()
    local_ip = socket.gethostbyname(hostname)
    
    print("🚢 Starting BattleShip API...")
    print("📍 API có thể truy cập tại:")
    print(f"   - http://localhost:8000")
    print(f"   - http://127.0.0.1:8000") 
    print(f"   - http://{local_ip}:8000 (LAN)")
    print("🔧 Test endpoint: POST /chat")
    print("🔥 Nhớ mở firewall cho port 8000!")
    
    # Truy cập từ mạng LAN
    uvicorn.run(app, host="0.0.0.0", port=8000)
