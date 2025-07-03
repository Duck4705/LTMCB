from langchain_ollama import OllamaEmbeddings
from langchain_community.vectorstores import Chroma
from langchain_core.documents import Document
import os
import json

# Đọc dữ liệu game từ file JSON
with open("ThongTinGame.json", 'r', encoding='utf-8') as file:
    game_data = json.load(file)

embeddings = OllamaEmbeddings(model="mxbai-embed-large")

db_location = "./game_db"
add_documents = not os.path.exists(db_location)

if add_documents:
    documents = []
    ids = []
    
    game_info = game_data['gameInfo']
    
    # 1. Thông tin cơ bản về game
    basic_content = f"Tên game: {game_info.get('title', 'N/A')}"
    basic_content += f" - Thể loại: {', '.join(game_info.get('genre', []))}"
    basic_content += f" - Mô tả: {game_info.get('description', 'N/A')}"
    
    documents.append(Document(
        page_content=basic_content,
        metadata={"source": "gameInfo", "type": "basic_info"},
        id="basic_info"
    ))
    ids.append("basic_info")
    
    # 2. Giai đoạn Setup
    if 'gamePhases' in game_info and 'setup' in game_info['gamePhases']:
        setup_info = game_info['gamePhases']['setup']
        setup_content = f"Giai đoạn Setup: {setup_info.get('description', 'N/A')}"
        setup_content += f" - Kích thước tàu: {', '.join(map(str, setup_info.get('shipSizes', [])))} ô"
        setup_content += f" - Hành động: {', '.join(setup_info.get('actions', []))}"
        
        documents.append(Document(
            page_content=setup_content,
            metadata={"source": "gamePhases", "type": "setup_phase"},
            id="setup_phase"
        ))
        ids.append("setup_phase")
    
    # 3. Giai đoạn Battle
    if 'gamePhases' in game_info and 'battle' in game_info['gamePhases']:
        battle_info = game_info['gamePhases']['battle']
        battle_content = f"Giai đoạn Battle: {battle_info.get('description', 'N/A')}"
        
        if 'mechanics' in battle_info:
            mechanics = battle_info['mechanics']
            battle_content += f" - Chế độ: {'Theo lượt' if mechanics.get('turnBased') else 'Thời gian thực'}"
            battle_content += f" - Hệ thống tấn công: {mechanics.get('attackSystem', 'N/A')}"
            battle_content += f" - Điều kiện thắng: {mechanics.get('winCondition', 'N/A')}"
        
        documents.append(Document(
            page_content=battle_content,
            metadata={"source": "gamePhases", "type": "battle_phase"},
            id="battle_phase"
        ))
        ids.append("battle_phase")
    
    # 4. Tính năng game
    if 'features' in game_info:
        features_content = f"Tính năng game: {', '.join(game_info['features'])}"
        
        documents.append(Document(
            page_content=features_content,
            metadata={"source": "gameInfo", "type": "features"},
            id="features"
        ))
        ids.append("features")
    
    # 5. Công nghệ sử dụng
    if 'technologies' in game_info:
        tech_info = game_info['technologies']
        tech_content = f"Công nghệ: {tech_info.get('mainLanguage', 'N/A')}"
        tech_content += f" - Game Engine: {tech_info.get('gameEngine', 'N/A')}"
        tech_content += f" - Nền tảng: {', '.join(tech_info.get('platforms', []))}"
        
        documents.append(Document(
            page_content=tech_content,
            metadata={"source": "gameInfo", "type": "technologies"},
            id="technologies"
        ))
        ids.append("technologies")

# Tạo vector store cho game
vector_store = Chroma(
    collection_name="battleship_collection",
    persist_directory=db_location,
    embedding_function=embeddings
)

if add_documents:
    vector_store.add_documents(documents=documents, ids=ids)
    
retriever = vector_store.as_retriever(
    search_kwargs={"k": 3}
)