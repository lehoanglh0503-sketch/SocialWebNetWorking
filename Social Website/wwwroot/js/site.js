// Connectify Global JavaScript Logic & Real-time Notifications

$(document).ready(function () {
    // Tự động xử lý tính năng Tắt/Bật hiện mật khẩu (Toggle Password Visibility)
    $(document).on('click', '.toggle-password-btn', function (e) {
        e.preventDefault();
        const btn = $(this);
        const targetId = btn.attr('data-target');
        let input;
        
        if (targetId) {
            input = $(targetId);
        } else {
            input = btn.closest('.input-group').find('input');
        }
        
        const icon = btn.find('i');
        
        if (input.attr('type') === 'password') {
            input.attr('type', 'text');
            icon.removeClass('bi-eye-fill bi-eye').addClass('bi-eye-slash-fill');
            btn.attr('title', 'Ẩn mật khẩu');
        } else {
            input.attr('type', 'password');
            icon.removeClass('bi-eye-slash-fill bi-eye-slash').addClass('bi-eye-fill');
            btn.attr('title', 'Hiện mật khẩu');
        }
    });

    // -------------------------------------------------------------
    // QUẢN LÝ THÔNG BÁO THỜI GIAN THỰC (REAL-TIME NOTIFICATIONS)
    // -------------------------------------------------------------
    const currentUserId = $('#currentUserIdVal').val();

    if (currentUserId && currentUserId !== "" && typeof signalR !== 'undefined') {
        // Tải số lượng thông báo chưa đọc ban đầu
        loadUnreadNotificationCount();

        // Khởi tạo kết nối SignalR toàn cục nếu chưa có
        if (!window.socialConnection) {
            window.socialConnection = new signalR.HubConnectionBuilder()
                .withUrl("/socialHub")
                .build();
        }

        if (window.socialConnection.state === signalR.HubConnectionState.Disconnected) {
            window.socialConnection.start().then(function () {
                if (currentUserId) {
                    window.socialConnection.invoke("JoinUserGroup", parseInt(currentUserId)).catch(err => console.error(err));
                }
            }).catch(err => console.error("SignalR Connection Error: ", err));
        } else if (window.socialConnection.state === signalR.HubConnectionState.Connected) {
            window.socialConnection.invoke("JoinUserGroup", parseInt(currentUserId)).catch(err => console.error(err));
        }

        // Lắng nghe sự kiện nhận thông báo mới qua SignalR
        window.socialConnection.off("ReceiveNotification");
        window.socialConnection.on("ReceiveNotification", function (notification) {
            // Tăng badge thông báo
            loadUnreadNotificationCount();

            // Nếu dropdown đang mở, chèn thông báo mới lên đầu list
            const listEl = $('#notificationList');
            if (listEl.length) {
                if (listEl.find('#notificationLoading').length || listEl.find('.bi-bell-slash').length) {
                    listEl.empty();
                }
                const itemHtml = renderNotificationItem(notification);
                listEl.prepend(itemHtml);
            }

            // Hiển thị Toast thông báo nhanh (Popup) ở góc màn hình
            showNotificationToast(notification);
        });

        // Khi người dùng bấm mở Dropdown thông báo -> Tải danh sách thông báo
        $('#dropdownNotification').on('show.bs.dropdown', function () {
            loadNotificationList();
        });

        // Đánh dấu 1 thông báo là đã đọc khi bấm vào
        $(document).on('click', '.notification-item', function (e) {
            const item = $(this);
            const id = item.data('id');
            const targetUrl = item.data('url');

            if (id) {
                $.post('/Notification/MarkAsRead', { id: id }, function (res) {
                    if (res.success) {
                        updateNotificationBadge(res.unreadCount);
                    }
                });
            }

            if (targetUrl && targetUrl !== '#') {
                window.location.href = targetUrl;
            }
        });

        // Đánh dấu tất cả đã đọc
        $('#btnMarkAllRead').on('click', function (e) {
            e.preventDefault();
            e.stopPropagation();
            $.post('/Notification/MarkAllAsRead', function (res) {
                if (res.success) {
                    updateNotificationBadge(0);
                    $('.notification-item').removeClass('unread');
                    $('.notification-item .unread-dot').remove();
                }
            });
        });
    }

    function loadUnreadNotificationCount() {
        $.get('/Notification/GetUnreadCount', function (res) {
            if (res.success) {
                updateNotificationBadge(res.unreadCount);
            }
        });
    }

    function updateNotificationBadge(count) {
        const badge = $('#notificationBadge');
        if (!badge.length) return;

        if (count > 0) {
            badge.text(count > 99 ? '99+' : count).removeClass('d-none');
        } else {
            badge.addClass('d-none').text('0');
        }
    }

    function loadNotificationList() {
        const listEl = $('#notificationList');
        listEl.html('<div class="text-center py-4 text-muted" id="notificationLoading"><div class="spinner-border spinner-border-sm text-primary me-2" role="status"></div> Đang tải thông báo...</div>');

        $.get('/Notification/GetNotifications', function (res) {
            if (res.success) {
                updateNotificationBadge(res.unreadCount);

                if (!res.notifications || res.notifications.length === 0) {
                    listEl.html('<div class="text-center py-4 text-muted small"><i class="bi bi-bell-slash fs-4 d-block mb-1"></i>Bạn chưa có thông báo nào</div>');
                    return;
                }

                let html = '';
                res.notifications.forEach(n => {
                    html += renderNotificationItem(n);
                });
                listEl.html(html);
            } else {
                listEl.html('<div class="text-center py-3 text-danger small">Không thể tải thông báo</div>');
            }
        });
    }

    function renderNotificationItem(n) {
        let iconHtml = '';
        switch (n.type) {
            case 'LikePost':
            case 'LikeComment':
                iconHtml = '<span class="notification-icon-badge bg-danger text-white"><i class="bi bi-heart-fill"></i></span>';
                break;
            case 'CommentPost':
            case 'ReplyComment':
                iconHtml = '<span class="notification-icon-badge bg-primary text-white"><i class="bi bi-chat-fill"></i></span>';
                break;
            case 'TagPost':
            case 'TagComment':
                iconHtml = '<span class="notification-icon-badge bg-warning text-white"><i class="bi bi-at"></i></span>';
                break;
            case 'FriendRequest':
            case 'FriendAccept':
                iconHtml = '<span class="notification-icon-badge bg-success text-white"><i class="bi bi-person-plus-fill"></i></span>';
                break;
            default:
                iconHtml = '<span class="notification-icon-badge bg-info text-white"><i class="bi bi-bell-fill"></i></span>';
                break;
        }

        const unreadClass = n.isRead ? '' : 'unread';
        const unreadDot = n.isRead ? '' : '<span class="unread-dot ms-auto my-auto"></span>';
        const avatarUrl = n.actor ? n.actor.avatarUrl : '/uploads/default-avatar.png';
        const actorName = n.actor ? n.actor.fullName : 'Người dùng';

        return `
            <div class="notification-item ${unreadClass} p-2 rounded-3 text-dark text-decoration-none d-flex align-items-center mb-1" data-id="${n.notificationId}" data-url="${n.targetUrl || '#'}">
                <div class="position-relative me-3 flex-shrink-0">
                    <img src="${avatarUrl}" alt="${actorName}" class="avatar avatar-sm rounded-circle" style="width: 42px; height: 42px; object-fit: cover;" />
                    ${iconHtml}
                </div>
                <div class="flex-grow-1 overflow-hidden me-2">
                    <div class="small fw-semibold text-dark text-break" style="font-size: 0.85rem; line-height: 1.3;">${n.message}</div>
                    <div class="text-muted text-nowrap" style="font-size: 0.75rem;">${n.timeAgo}</div>
                </div>
                ${unreadDot}
            </div>
        `;
    }

    function showNotificationToast(n) {
        let toastContainer = $('#notificationToastContainer');
        if (!toastContainer.length) {
            $('body').append('<div id="notificationToastContainer" class="toast-container position-fixed bottom-0 end-0 p-3" style="z-index: 1090;"></div>');
            toastContainer = $('#notificationToastContainer');
        }

        const toastId = 'toast-' + Date.now();
        const avatarUrl = n.actor ? n.actor.avatarUrl : '/uploads/default-avatar.png';

        const toastHtml = `
            <div id="${toastId}" class="toast bg-white border-0 shadow-lg rounded-4 overflow-hidden mb-2 animate-fade-in-up" role="alert" aria-live="assertive" aria-atomic="true">
                <div class="toast-body p-3 d-flex align-items-center cursor-pointer" onclick="window.location.href='${n.targetUrl || '#'}'">
                    <img src="${avatarUrl}" class="avatar rounded-circle me-3 flex-shrink-0" style="width: 40px; height: 40px;" />
                    <div class="flex-grow-1 me-2">
                        <div class="fw-bold text-primary mb-0" style="font-size: 0.8rem;"><i class="bi bi-bell-fill me-1"></i>Thông báo mới</div>
                        <div class="text-dark small fw-semibold">${n.message}</div>
                    </div>
                    <button type="button" class="btn-close ms-auto align-self-start" data-bs-dismiss="toast" aria-label="Close"></button>
                </div>
            </div>
        `;

        toastContainer.append(toastHtml);
        const toastEl = document.getElementById(toastId);
        const toast = new bootstrap.Toast(toastEl, { delay: 5000 });
        toast.show();
    }
});
