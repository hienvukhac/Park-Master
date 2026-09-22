# 🚗 Park Master 3D - Unity Puzzle Game

Dự án game 3D Casual / Puzzle phát triển trên nền tảng **Unity (URP)**. Người chơi chạm vuốt vẽ quỹ đạo di chuyển để điều khiển các xe đỗ vào đúng ô màu tương ứng, tính toán thời gian tránh va chạm, thu thập tiền xu và mở khóa xe mới trong cửa hàng.

---

## 🎬 Video Demo & Gameplay Showcase

| 🎮 1. In-Game Gameplay Demo | 🛠️ 2. Unity Editor & Scene Overview |
| :--- | :--- |
| Trải nghiệm cơ chế vẽ đường, xe di chuyển, nhặt xu và chiến thắng. | Cấu trúc Scene, Prefab Level trong Resources, Inspector và Scripts. |
| 📺 **[▶️ BẤM VÀO ĐÂY ĐỂ XEM GAMEPLAY DEMO](https://github.com/hienvukhac/Park-Master/raw/master/Docs/gameplay.mp4)** | 📺 **[▶️ BẤM VÀO ĐÂY ĐỂ XEM EDITOR SCENE DEMO](https://github.com/hienvukhac/Park-Master/raw/master/Docs/unity-editor-scene-demo.mp4)** |

---

## 🎮 Tính năng nổi bật (Key Features)

- **Cơ chế vẽ đường tự do (Line Drawing)**: Kéo thả ngón tay hoặc chuột để định tuyến đường đi linh hoạt cho từng chiếc xe thông qua `LineRenderer`.
- **Hệ thống phân loại màu sắc (Color Matching)**: Kiểm tra điều kiện xe phải đỗ chính xác vào ô màu tương ứng (`ColorType`).
- **Hệ thống kinh tế & Vật phẩm (Collectibles & Economy)**:
  - Nhặt tiền xu xoay 3D (`CoinItem`, `CoinSpinner`) dọc đường đi.
  - Thu thập chìa khóa (`Key`) mở khóa phần thưởng.
- **Cửa hàng xe (Car Shop)**: Tích lũy xu để mở khóa các mẫu xe 3D đa dạng (Audi R8, Jeep Renegade 2016, Lamborghini).
- **Hệ thống Level động (Dynamic Level Management)**: Tải và nạp màn chơi tự động từ `Resources/Levels/` mà không cần load lại Scene.
- **Hiệu ứng trực quan & Hoạt ảnh (VFX & DOTween)**: Hiệu ứng pháo hoa ăn mừng khi thắng màn, tia lửa va chạm và hoạt ảnh bay tiền xu UI mượt mà.

---

## 🏗️ Kiến trúc & Công nghệ (Tech Stack)

- **Engine**: Unity 6 / Unity 2022.3 LTS (Universal Render Pipeline - URP).
- **Plugins & Libraries**:
  - **DOTween (HOTween v2)**: Xử lý tweening chuyển động xe và animation UI.
  - **TextMesh Pro**: Hiển thị văn bản UI sắc nét chuẩn vector.
- **Design Patterns**:
  - `Singleton Pattern`: Quản lý các Manager trung tâm (`GameManager`, `CoinManager`, `LevelManager`, `CarShopManager`).
  - `Object Pooling`: Tối ưu hiệu năng tái tạo hiệu ứng hạt VFX và tiền xu.
  - `ScriptableObject Database`: Quản lý danh mục thông số xe `CarDatabase`.
  - `State Pattern`: Quản lý vòng lặp trạng thái game (`Drawing`, `Moving`, `Win`, `GameOver`).

---

## 📂 Cấu trúc Thư mục Dự Án (Project Structure)

```text
Park-Master/
├── Assets/
│   ├── Materials/         # Material và Shader URP
│   ├── Models/            # Model xe 3D, tiền xu
│   ├── Plugins/           # Thư viện DOTween
│   ├── Prefabs/           # Prefab xe, slot đỗ xe, hiệu ứng VFX
│   ├── Resources/         # Database xe và Prefab các Level (1-4)
│   ├── Scenes/            # Scene gameplay chính 
│   ├── Scripts/           # Toàn bộ mã nguồn C# tổ chức theo module
│   └── Sprites/           # Icon UI, texture môi trường
├── Docs/                  # Chứa 2 video demo (gameplay.mp4 & unity-editor-scene-demo.mp4)
├── Packages/              # Unity Package Manager manifest
└── ProjectSettings/       # Cấu hình dự án Unity
