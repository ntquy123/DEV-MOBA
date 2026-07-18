# DEV MOBA

## Quy ước quản lý model tướng

Thư mục `Assets/Model/MOBACharacter/` chỉ dùng để lưu **model nguồn của tướng** trong máy local. Thư mục này đã được bỏ qua bởi Git, bao gồm cả các file `.meta` do Unity tạo.

- Model do ai cung cấp thì người đó quản lý file nguồn, quyền sử dụng và giấy phép của model đó.
- Không commit, push, gửi kèm repository, hoặc chia sẻ lại model nguồn khi chưa có sự cho phép rõ ràng của chủ sở hữu.
- Khi cần đưa tướng vào game, hãy đánh dấu asset phù hợp là **Addressable**, build thành Addressables bundle và chỉ upload bundle đã build lên server/CDN.
- Repository chỉ lưu code, cấu hình Addressables, prefab/metadata không chứa model nguồn (nếu được phép), và tài liệu kỹ thuật cần thiết để tải bundle.

> Lưu ý: `.gitignore` và Addressables bundle không tự tạo hoặc thay thế quyền tác giả. Trước khi phân phối bundle, vẫn cần bảo đảm người đóng góp có quyền cho phép build, lưu trữ trên server và phân phối asset đó cho người chơi.
