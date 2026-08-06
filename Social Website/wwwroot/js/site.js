// Connectify Global JavaScript Logic

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
});
