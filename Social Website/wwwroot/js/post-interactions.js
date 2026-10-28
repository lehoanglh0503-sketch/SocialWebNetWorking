// Connectify Post Interactions & SignalR Handlers

$(document).ready(function () {
            const currentAvatarUrl = window.currentAvatarUrl || "";
            const currentFullName = window.currentFullName || "";

            // 1. Kết nối tới SignalR SocialHub
            const connection = new signalR.HubConnectionBuilder()
                .withUrl("/socialHub")
                .build();

            // Nhận bài đăng mới
            connection.on("ReceiveNewPost", function (post) {
                const isOwnPost = post.userId == window.currentUserId || post.username == window.currentUsername;
                const isAdmin = window.isCurrentUserAdmin === true;
                const canDelete = isOwnPost || isAdmin;
                
                const dropdownItemsHtml = canDelete ? `
                    <li><a class="dropdown-item" href="#"><i class="bi bi-bookmark me-2"></i>Lưu bài viết</a></li>
                    <li><hr class="dropdown-divider"></li>
                    <li><a class="dropdown-item text-danger btn-delete-post-trigger" href="#" data-post-id="${post.postId}"><i class="bi bi-trash3-fill me-2"></i>Xóa bài viết</a></li>
                ` : `
                    <li><a class="dropdown-item" href="#"><i class="bi bi-bookmark me-2"></i>Lưu bài viết</a></li>
                    <li><a class="dropdown-item text-warning btn-report-post-trigger" href="#" data-post-id="${post.postId}"><i class="bi bi-flag me-2"></i>Báo cáo bài viết</a></li>
                `;

                const dropdownHtml = `
                    <div class="dropdown">
                        <button class="btn btn-link text-muted p-0" data-bs-toggle="dropdown">
                            <i class="bi bi-three-dots"></i>
                        </button>
                        <ul class="dropdown-menu dropdown-menu-end shadow border-0">
                            ${dropdownItemsHtml}
                        </ul>
                    </div>
                `;

                const contentHtml = post.content ? `<p class="card-text fs-6 mb-3" style="white-space: pre-wrap;">${post.content}</p>` : '';
                let imageHtml = '';
                if (post.imageUrl) {
                    const imgList = post.imageUrl.split(',').filter(x => x);
                    if (imgList.length === 1) {
                        imageHtml = `<div class="post-image-container mb-3 text-center"><img src="${imgList[0]}" alt="Ảnh bài viết" class="img-fluid rounded shadow-sm" style="max-height: 500px; width: auto; object-fit: cover; cursor: pointer;" onclick="window.open('${imgList[0]}')" /></div>`;
                    } else if (imgList.length > 1) {
                        const colClass = imgList.length === 2 ? 'col-6' : (imgList.length === 3 ? 'col-4' : 'col-6');
                        const imgsHtml = imgList.map(img => `<div class="${colClass}"><img src="${img}" class="img-fluid rounded shadow-sm w-100" style="height: 200px; object-fit: cover; cursor: pointer;" onclick="window.open('${img}')" /></div>`).join('');
                        imageHtml = `<div class="post-images-grid mb-3 row g-2">${imgsHtml}</div>`;
                    }
                }

                let videoHtml = '';
                if (post.videoUrl) {
                    videoHtml = `
                        <div class="post-video-container mb-3 text-center">
                            <video controls preload="metadata" class="w-100 rounded shadow-sm" style="max-height: 500px;">
                                <source src="${post.videoUrl}" type="video/mp4">
                                <source src="${post.videoUrl}" type="video/webm">
                                Trình duyệt không hỗ trợ thẻ video HTML5.
                            </video>
                        </div>
                    `;
                }

                const postHtml = `
                    <div class="card card-hover mb-4 animate-fade-in-up" id="postCard-${post.postId}">
                        <div class="card-body p-4">
                            <div class="d-flex justify-content-between align-items-center mb-3">
                                <div class="d-flex align-items-center">
                                    <img src="${post.avatarUrl}" alt="${post.fullName}" class="avatar avatar-sm me-3" />
                                    <div>
                                        <h6 class="fw-bold mb-0">${post.fullName}</h6>
                                        <span class="text-muted small">${post.createdAt}</span>
                                    </div>
                                </div>
                                ${dropdownHtml}
                            </div>
                            ${contentHtml}
                            ${imageHtml}
                            ${videoHtml}
                            
                            <div class="d-flex justify-content-between text-muted small border-top border-bottom py-2 my-2">
                                <span id="likeCount-${post.postId}" class="cursor-pointer btn-view-reactions hover-underline" data-post-id="${post.postId}">
                                    <span class="reactions-emojis-container me-1"></span>
                                    <span class="reactions-count-text">0 lượt thích</span>
                                </span>
                                <span id="commentCount-${post.postId}"><i class="bi bi-chat-dots me-1"></i> 0 bình luận</span>
                            </div>
                            
                            <div class="d-flex justify-content-around mb-3 position-relative">
                                <div class="w-50 position-relative reaction-button-container">
                                    <button class="btn btn-link text-decoration-none text-muted fw-semibold py-1 w-100 btn-toggle-like" data-post-id="${post.postId}" data-current-reaction="">
                                        <i class="bi bi-hand-thumbs-up me-1"></i><span>Thích</span>
                                    </button>
                                    
                                    <div class="reactions-flyout shadow border-0 rounded-pill p-2 d-none position-absolute animate-fade-in-up">
                                        <div class="d-flex align-items-center gap-2 px-1">
                                            <span class="reaction-emoji-item" data-reaction="Like" data-post-id="${post.postId}" title="Thích">👍</span>
                                            <span class="reaction-emoji-item" data-reaction="Love" data-post-id="${post.postId}" title="Yêu thích">❤️</span>
                                            <span class="reaction-emoji-item" data-reaction="Haha" data-post-id="${post.postId}" title="Haha">😆</span>
                                            <span class="reaction-emoji-item" data-reaction="Wow" data-post-id="${post.postId}" title="Wow">😮</span>
                                            <span class="reaction-emoji-item" data-reaction="Sad" data-post-id="${post.postId}" title="Buồn">😢</span>
                                            <span class="reaction-emoji-item" data-reaction="Angry" data-post-id="${post.postId}" title="Phẫn nộ">😡</span>
                                        </div>
                                    </div>
                                </div>
                                <button class="btn btn-link text-decoration-none text-muted fw-semibold py-1 w-50 hover-primary" onclick="$('#commentSection-${post.postId}').toggle();"><i class="bi bi-chat-dots me-1"></i>Bình luận</button>
                            </div>
                            <div class="comment-section" id="commentSection-${post.postId}">
                                <div class="comment-list d-flex flex-column gap-2 mb-3" id="commentList-${post.postId}"></div>
                                <form class="create-comment-form" data-post-id="${post.postId}">
                                    <div class="comment-input-wrapper d-flex align-items-center">
                                        <img src="${currentAvatarUrl}" alt="${currentFullName}" class="avatar avatar-sm me-2" />
                                        <div class="input-group">
                                            <input type="text" class="form-control comment-input" placeholder="Viết bình luận..." required autocomplete="off" />
                                            <button type="submit" class="comment-send-btn"><i class="bi bi-send-fill"></i></button>
                                        </div>
                                    </div>
                                </form>
                            </div>
                        </div>
                    </div>
                `;
                $("#feedContainer").prepend(postHtml);

                if (isOwnPost) {
                    const countBadge = $('.user-post-count-badge');
                    if (countBadge.length > 0) {
                        let count = parseInt(countBadge.first().text()) || 0;
                        countBadge.text(count + 1);
                    }
                }
            });

            // Nhận bình luận mới (hỗ trợ cả bình luận gốc và phản hồi bình luận)
            connection.on("ReceiveNewComment", function (comment) {
                // Ngăn chặn lặp trùng bình luận nếu đã có trong DOM
                if ($(`#commentItem-${comment.commentId}`).length > 0) return;

                const currentUserId = window.currentUserId || $("#currentUserIdVal").val();
                const isCurrentUserAdmin = window.isCurrentUserAdmin === true;
                const canDelete = (comment.userId == currentUserId || comment.postAuthorUserId == currentUserId || isCurrentUserAdmin);

                const deleteBtnHtml = canDelete ? `
                    <button type="button" class="btn btn-link text-danger p-0 ms-1 btn-delete-comment-trigger" data-comment-id="${comment.commentId}" title="Xóa bình luận" style="font-size: 0.8rem; line-height: 1;">
                        <i class="bi bi-trash3"></i>
                    </button>
                ` : '';

                const parentId = comment.parentCommentId || comment.ParentCommentId;

                if (parentId && parentId != 0) {
                    let $replyList = $(`#replyList-${parentId}`);
                    if ($replyList.length === 0) {
                        $(`#commentItem-${parentId}`).append(`
                            <div class="reply-list ms-4 ps-3 border-start mt-2 d-flex flex-column gap-2" id="replyList-${parentId}"></div>
                        `);
                        $replyList = $(`#replyList-${parentId}`);
                    }

                    const replyHtml = `
                        <div class="d-flex position-relative animate-fade-in-up" id="commentItem-${comment.commentId}">
                            <img src="${comment.avatarUrl}" alt="${comment.fullName}" class="avatar avatar-xs me-2 mt-1" style="width: 28px; height: 28px;" />
                            <div class="comment-box w-100 position-relative bg-light p-2 rounded">
                                <div class="d-flex justify-content-between align-items-center">
                                    <span class="fw-bold small">${comment.fullName}</span>
                                    <div class="d-flex align-items-center gap-2">
                                        <span class="text-muted" style="font-size: 0.72rem;">${comment.createdAt}</span>
                                        ${deleteBtnHtml}
                                    </div>
                                </div>
                                <p class="mb-1 mt-1 small">${comment.content}</p>
                                <div class="d-flex align-items-center gap-3 pt-1 border-top border-light mt-1 position-relative">
                                    <div class="position-relative comment-reaction-container">
                                        <button type="button" class="btn btn-link p-0 text-decoration-none text-muted small btn-toggle-comment-like" data-comment-id="${comment.commentId}" style="font-size: 0.75rem;">
                                            <i class="bi bi-hand-thumbs-up me-1"></i><span>Thích</span>
                                        </button>
                                        <div class="comment-reactions-flyout shadow border-0 rounded-pill p-1 d-none position-absolute animate-fade-in-up" style="bottom: 100%; left: 0; z-index: 1050; background: white;">
                                            <div class="d-flex align-items-center gap-1.5 px-1">
                                                <span class="comment-reaction-emoji-item" data-reaction="Like" data-comment-id="${comment.commentId}" title="Thích" style="cursor: pointer; font-size: 1rem;">👍</span>
                                                <span class="comment-reaction-emoji-item" data-reaction="Love" data-comment-id="${comment.commentId}" title="Yêu thích" style="cursor: pointer; font-size: 1rem;">❤️</span>
                                                <span class="comment-reaction-emoji-item" data-reaction="Haha" data-comment-id="${comment.commentId}" title="Haha" style="cursor: pointer; font-size: 1rem;">😆</span>
                                                <span class="comment-reaction-emoji-item" data-reaction="Wow" data-comment-id="${comment.commentId}" title="Wow" style="cursor: pointer; font-size: 1rem;">😮</span>
                                                <span class="comment-reaction-emoji-item" data-reaction="Sad" data-comment-id="${comment.commentId}" title="Buồn" style="cursor: pointer; font-size: 1rem;">😢</span>
                                                <span class="comment-reaction-emoji-item" data-reaction="Angry" data-comment-id="${comment.commentId}" title="Phẫn nộ" style="cursor: pointer; font-size: 1rem;">😡</span>
                                            </div>
                                        </div>
                                    </div>

                                    <span class="comment-like-count cursor-pointer btn-view-comment-reactions hover-underline d-none" data-comment-id="${comment.commentId}" style="font-size: 0.72rem;">
                                        <span class="comment-emojis-container me-1"></span>
                                        <span class="comment-count-num fw-semibold">0</span>
                                    </span>

                                    <button type="button" class="btn btn-link p-0 text-decoration-none text-muted small btn-reply-comment-trigger" data-comment-id="${parentId}" data-user-name="${comment.fullName}" data-post-id="${comment.postId}" style="font-size: 0.75rem;">
                                        <i class="bi bi-reply-fill me-1"></i>Trả lời
                                    </button>
                                </div>
                            </div>
                        </div>
                    `;
                    $replyList.append(replyHtml);
                } else {
                    const commentHtml = `
                        <div class="d-flex flex-column mb-2 animate-fade-in-up" id="commentItem-${comment.commentId}">
                            <div class="d-flex position-relative">
                                <img src="${comment.avatarUrl}" alt="${comment.fullName}" class="avatar avatar-sm me-2 mt-1" />
                                <div class="comment-box w-100 position-relative">
                                    <div class="d-flex justify-content-between align-items-center">
                                        <span class="fw-bold">${comment.fullName}</span>
                                        <div class="d-flex align-items-center gap-2">
                                            <span class="text-muted small">${comment.createdAt}</span>
                                            ${deleteBtnHtml}
                                        </div>
                                    </div>
                                    <p class="mb-1 mt-1">${comment.content}</p>
                                    <div class="d-flex align-items-center gap-3 pt-1 border-top border-light mt-1 position-relative">
                                        <div class="position-relative comment-reaction-container">
                                            <button type="button" class="btn btn-link p-0 text-decoration-none text-muted small btn-toggle-comment-like" data-comment-id="${comment.commentId}" style="font-size: 0.78rem;">
                                                <i class="bi bi-hand-thumbs-up me-1"></i><span>Thích</span>
                                            </button>
                                            <div class="comment-reactions-flyout shadow border-0 rounded-pill p-1 d-none position-absolute animate-fade-in-up" style="bottom: 100%; left: 0; z-index: 1050; background: white;">
                                                <div class="d-flex align-items-center gap-1.5 px-1">
                                                    <span class="comment-reaction-emoji-item" data-reaction="Like" data-comment-id="${comment.commentId}" title="Thích" style="cursor: pointer; font-size: 1.1rem;">👍</span>
                                                    <span class="comment-reaction-emoji-item" data-reaction="Love" data-comment-id="${comment.commentId}" title="Yêu thích" style="cursor: pointer; font-size: 1.1rem;">❤️</span>
                                                    <span class="comment-reaction-emoji-item" data-reaction="Haha" data-comment-id="${comment.commentId}" title="Haha" style="cursor: pointer; font-size: 1.1rem;">😆</span>
                                                    <span class="comment-reaction-emoji-item" data-reaction="Wow" data-comment-id="${comment.commentId}" title="Wow" style="cursor: pointer; font-size: 1.1rem;">😮</span>
                                                    <span class="comment-reaction-emoji-item" data-reaction="Sad" data-comment-id="${comment.commentId}" title="Buồn" style="cursor: pointer; font-size: 1.1rem;">😢</span>
                                                    <span class="comment-reaction-emoji-item" data-reaction="Angry" data-comment-id="${comment.commentId}" title="Phẫn nộ" style="cursor: pointer; font-size: 1.1rem;">😡</span>
                                                </div>
                                            </div>
                                        </div>

                                        <span class="comment-like-count cursor-pointer btn-view-comment-reactions hover-underline d-none" data-comment-id="${comment.commentId}" style="font-size: 0.75rem;">
                                            <span class="comment-emojis-container me-1"></span>
                                            <span class="comment-count-num fw-semibold">0</span>
                                        </span>

                                        <button type="button" class="btn btn-link p-0 text-decoration-none text-muted small btn-reply-comment-trigger" data-comment-id="${comment.commentId}" data-user-name="${comment.fullName}" data-post-id="${comment.postId}" style="font-size: 0.78rem;">
                                            <i class="bi bi-reply-fill me-1"></i>Trả lời
                                        </button>
                                    </div>
                                </div>
                            </div>
                            <div class="reply-list ms-4 ps-3 border-start mt-2 d-flex flex-column gap-2" id="replyList-${comment.commentId}"></div>
                            <div class="reply-input-container ms-4 ps-3 border-start mt-2 d-none" id="replyFormContainer-${comment.commentId}">
                                <form class="create-comment-form d-flex align-items-center gap-2" data-post-id="${comment.postId}" data-parent-id="${comment.commentId}">
                                    <img src="${currentAvatarUrl}" alt="${currentFullName}" class="avatar avatar-xs me-1" style="width: 28px; height: 28px;" />
                                    <div class="input-group input-group-sm w-100">
                                        <input type="text" class="form-control comment-input reply-input" placeholder="Trả lời bình luận..." required autocomplete="off" />
                                        <button type="submit" class="btn btn-primary btn-sm px-3"><i class="bi bi-send-fill"></i></button>
                                    </div>
                                </form>
                            </div>
                        </div>
                    `;
                    $(`#commentList-${comment.postId}`).append(commentHtml);
                }

                // Cập nhật số lượng bình luận
                const $count = $(`#commentCount-${comment.postId}`);
                const match = $count.text().match(/\d+/);
                if (match) {
                    const currentCount = parseInt(match[0]);
                    $count.html(`<i class="bi bi-chat-dots me-1"></i> ${currentCount + 1} bình luận`);
                }
            });

            // Nhận cập nhật lượt thích / thả cảm xúc bình luận qua SignalR
            connection.on("UpdateCommentLikes", function (commentId, postId, totalCount, topReactions) {
                const commentItem = $(`#commentItem-${commentId}`);
                if (commentItem.length > 0) {
                    const countSpan = commentItem.find(".comment-like-count").first();
                    if (countSpan.length > 0) {
                        const emojiContainer = countSpan.find(".comment-emojis-container");
                        emojiContainer.empty();
                        
                        const emojiMap = {
                            "Like": "👍",
                            "Love": "❤️",
                            "Haha": "😆",
                            "Wow": "😮",
                            "Sad": "😢",
                            "Angry": "😡"
                        };
                        
                        if (topReactions && topReactions.length > 0) {
                            topReactions.forEach(function (r) {
                                emojiContainer.append(`<span>${emojiMap[r] || "👍"}</span>`);
                            });
                        } else {
                            emojiContainer.append(`<span>👍</span>`);
                        }
                        
                        countSpan.find(".comment-count-num").text(totalCount);
                        
                        if (totalCount > 0) {
                            countSpan.removeClass("d-none");
                        } else {
                            countSpan.addClass("d-none");
                            updateCommentLikeButtonUI(commentId, false, null);
                        }
                    }
                }
            });

            // Nhận sự kiện xóa bình luận qua SignalR
            connection.on("ReceiveDeletedComment", function (commentId, postId) {
                $(`#commentItem-${commentId}`).fadeOut(300, function () {
                    $(this).remove();
                });

                const $count = $(`#commentCount-${postId}`);
                const match = $count.text().match(/\d+/);
                if (match) {
                    const currentCount = parseInt(match[0]);
                    const newCount = Math.max(0, currentCount - 1);
                    $count.html(`<i class="bi bi-chat-dots me-1"></i> ${newCount} bình luận`);
                }
            });

            // 3. Gửi bình luận hoặc trả lời bình luận bằng AJAX
            $(document).on("submit", ".create-comment-form", function (e) {
                e.preventDefault();
                e.stopImmediatePropagation();
                const form = $(this);
                const postId = form.attr("data-post-id") || form.data("postId") || form.data("post-id");
                const parentId = form.attr("data-parent-id") || form.data("parentId") || form.data("parent-id") || null;
                const $input = form.find(".comment-input");
                const content = $input.val();

                if (!content || content.trim().length === 0) return;

                const $submitBtn = form.find("button[type='submit']");
                if ($submitBtn.prop("disabled")) return;
                $submitBtn.prop("disabled", true);

                $.post("/Post/CreateComment", { postId: postId, content: content, parentCommentId: parentId }, function (res) {
                    $submitBtn.prop("disabled", false);
                    if (res.success) {
                        $input.val("");
                        if (parentId) {
                            $(`#replyFormContainer-${parentId}`).addClass("d-none");
                        }
                    } else {
                        alert(res.message);
                    }
                }).fail(function () {
                    $submitBtn.prop("disabled", false);
                });
            });

            // Gửi cảm xúc bình luận lên server
            function sendCommentReaction(commentId, reactionType) {
                $.post("/Post/ToggleCommentLike", { commentId: commentId, reactionType: reactionType }, function (res) {
                    if (res.success) {
                        updateCommentLikeButtonUI(commentId, res.isLiked, res.reactionType);
                    } else {
                        alert(res.message);
                    }
                });
            }

            // Cập nhật UI nút Thích của Bình luận
            function updateCommentLikeButtonUI(commentId, isLiked, reactionType) {
                const btn = $(`.btn-toggle-comment-like[data-comment-id="${commentId}"]`);
                if (btn.length === 0) return;
                
                btn.removeClass("text-muted text-primary text-danger text-warning fw-bold");
                
                if (isLiked) {
                    btn.attr("data-current-reaction", reactionType);
                    
                    let btnClass = "text-muted";
                    let btnIcon = "bi-hand-thumbs-up";
                    let btnText = "Thích";
                    
                    switch (reactionType) {
                        case "Like":
                            btnClass = "text-primary fw-bold";
                            btnIcon = "bi-hand-thumbs-up-fill";
                            btnText = "Thích";
                            break;
                        case "Love":
                            btnClass = "text-danger fw-bold";
                            btnIcon = "bi-heart-fill";
                            btnText = "Yêu thích";
                            break;
                        case "Haha":
                            btnClass = "text-warning fw-bold";
                            btnIcon = "bi-emoji-laughing-fill";
                            btnText = "Haha";
                            break;
                        case "Wow":
                            btnClass = "text-warning fw-bold";
                            btnIcon = "bi-emoji-surprise-fill";
                            btnText = "Wow";
                            break;
                        case "Sad":
                            btnClass = "text-warning fw-bold";
                            btnIcon = "bi-emoji-frown-fill";
                            btnText = "Buồn";
                            break;
                        case "Angry":
                            btnClass = "text-danger fw-bold";
                            btnIcon = "bi-emoji-angry-fill";
                            btnText = "Phẫn nộ";
                            break;
                    }
                    
                    btn.addClass(btnClass);
                    btn.find("i").attr("class", `bi ${btnIcon} me-1`);
                    btn.find("span").text(btnText);
                } else {
                    btn.attr("data-current-reaction", "");
                    btn.addClass("text-muted");
                    btn.find("i").attr("class", "bi bi-hand-thumbs-up me-1");
                    btn.find("span").text("Thích");
                }
            }

            // Rê chuột hiển thị Flyout Emojis cho Bình luận
            $(document).on("mouseenter", ".comment-reaction-container", function () {
                $(this).find(".comment-reactions-flyout").removeClass("d-none");
            }).on("mouseleave", ".comment-reaction-container", function () {
                $(this).find(".comment-reactions-flyout").addClass("d-none");
            });

            // Click trực tiếp nút Thích bình luận (Toggle mặc định)
            $(document).on("click", ".btn-toggle-comment-like", function () {
                const commentId = $(this).data("comment-id");
                const currentReaction = $(this).attr("data-current-reaction");
                const reactionToToggle = currentReaction ? currentReaction : "Like";
                
                sendCommentReaction(commentId, reactionToToggle);
            });

            // Click chọn cảm xúc cụ thể cho Bình luận
            $(document).on("click", ".comment-reaction-emoji-item", function (e) {
                e.stopPropagation();
                const commentId = $(this).data("comment-id");
                const reaction = $(this).data("reaction");
                
                sendCommentReaction(commentId, reaction);
                $(this).closest(".comment-reactions-flyout").addClass("d-none");
            });

            // Bấm xem ai đã thả cảm xúc cho Bình luận
            $(document).on("click", ".btn-view-comment-reactions", function () {
                const commentId = $(this).data("comment-id");
                
                $("#commentReactionsModalBody").html('<div class="text-center py-4"><span class="spinner-border spinner-border-sm text-muted"></span></div>');
                $("#commentReactionsDetailModal").modal("show");
                
                $.get("/Post/GetCommentReactions", { commentId: commentId }, function (res) {
                    if (res.success) {
                        $("#commentReactionsModalBody").empty();
                        
                        if (res.reactions.length === 0) {
                            $("#commentReactionsModalBody").html('<div class="text-center text-muted small py-3">Chưa có ai bày tỏ cảm xúc.</div>');
                            return;
                        }
                        
                        const emojiMap = {
                            "Like": "👍",
                            "Love": "❤️",
                            "Haha": "😆",
                            "Wow": "😮",
                            "Sad": "😢",
                            "Angry": "😡"
                        };
                        
                        res.reactions.forEach(function (item) {
                            const emoji = emojiMap[item.reactionType] || "👍";
                            const rowHtml = `
                                <div class="d-flex align-items-center justify-content-between py-2 border-bottom border-light">
                                    <div class="d-flex align-items-center">
                                        <img src="${item.avatarUrl}" alt="${item.fullName}" class="avatar avatar-sm me-2.5" />
                                        <div>
                                            <h6 class="fw-semibold mb-0 small text-truncate" style="max-width: 180px;">${item.fullName}</h6>
                                            <span class="text-muted small" style="font-size: 0.7rem;">@${item.username}</span>
                                        </div>
                                    </div>
                                    <span style="font-size: 1.2rem;">${emoji}</span>
                                </div>
                            `;
                            $("#commentReactionsModalBody").append(rowHtml);
                        });
                    }
                });
            });

            // Bấm 'Trả lời' mở khung nhập phản hồi
            $(document).on("click", ".btn-reply-comment-trigger", function () {
                const commentId = $(this).attr("data-comment-id") || $(this).data("commentId") || $(this).data("comment-id");
                const userName = $(this).attr("data-user-name") || $(this).data("userName") || $(this).data("user-name");
                const container = $(`#replyFormContainer-${commentId}`);
                
                container.removeClass("d-none");
                const input = container.find(".reply-input");
                if (userName) {
                    input.val('@' + userName + ' ');
                }
                input.focus();
            });

            // Nhận cập nhật tương tác thời gian thực
            connection.on("UpdatePostReactions", function (postId, totalCount, topReactions) {
                const countSpan = $(`#likeCount-${postId}`);
                if (countSpan.length > 0) {
                    const emojiContainer = countSpan.find(".reactions-emojis-container");
                    emojiContainer.empty();
                    
                    const emojiMap = {
                        "Like": "👍",
                        "Love": "❤️",
                        "Haha": "😆",
                        "Wow": "😮",
                        "Sad": "😢",
                        "Angry": "😡"
                    };
                    
                    topReactions.forEach(function (r) {
                        emojiContainer.append(`<span>${emojiMap[r] || "👍"}</span>`);
                    });
                    
                    countSpan.find(".reactions-count-text").text(`${totalCount} lượt thích`);
                    
                    // Cập nhật lại UI nút Thích cho khớp với số lượt thích mới nhận được qua SignalR
                    // Nếu là 0 lượt thích, xóa cảm xúc của nút hiện tại nếu có
                    if (totalCount === 0) {
                        updateLikeButtonUI(postId, false, null);
                    }
                }
            });

            connection.start().catch(err => console.error(err.toString()));

            // Ẩn cảnh báo khi người dùng bắt đầu sửa nội dung
            $("#postContent").on("input", function () {
                $("#postValidationAlert").addClass("d-none");
            });

            // Quản lý danh sách file ảnh đính kèm bài viết (Tối đa 5 ảnh)
            let selectedPostFiles = [];

            $("#postImageInput").on("change", function () {
                const files = Array.from(this.files);
                if (files.length > 0) {
                    const allowedTypes = ['image/jpeg', 'image/png', 'image/webp', 'image/gif'];
                    const validFiles = files.filter(f => allowedTypes.includes(f.type));

                    if (validFiles.length < files.length) {
                        alert("Chỉ chấp nhận các file ảnh định dạng JPG, PNG, WEBP, GIF.");
                    }

                    selectedPostFiles = selectedPostFiles.concat(validFiles).slice(0, 5);
                    renderPostImagePreviews();
                }
            });

            function renderPostImagePreviews() {
                const container = $("#postImagePreviewContainer");
                container.empty();

                if (selectedPostFiles.length === 0) {
                    $("#postImagePreviewWrapper").addClass("d-none");
                    return;
                }

                $("#postImagePreviewWrapper").removeClass("d-none");
                const colClass = selectedPostFiles.length === 1 ? 'col-12' : (selectedPostFiles.length === 2 ? 'col-6' : 'col-4');

                selectedPostFiles.forEach((file, index) => {
                    const reader = new FileReader();
                    reader.onload = function (e) {
                        const itemHtml = `
                            <div class="${colClass} position-relative">
                                <img src="${e.target.result}" class="img-fluid rounded shadow-sm w-100" style="height: 120px; object-fit: cover;" />
                                <button type="button" class="btn btn-sm btn-dark position-absolute top-0 end-0 m-1 rounded-circle btn-remove-preview-img" data-index="${index}" style="width: 24px; height: 24px; padding: 0; line-height: 1;" title="Xóa ảnh">
                                    <i class="bi bi-x-lg" style="font-size: 0.75rem;"></i>
                                </button>
                            </div>
                        `;
                        container.append(itemHtml);
                    };
                    reader.readAsDataURL(file);
                });
            }

            $(document).on("click", ".btn-remove-preview-img", function () {
                const idx = parseInt($(this).data("index"));
                selectedPostFiles.splice(idx, 1);
                renderPostImagePreviews();
            });

            // Quản lý file video đính kèm bài viết (Tối đa 1 video, max 25MB)
            let selectedPostVideo = null;

            $("#postVideoInput").on("change", function () {
                const file = this.files[0];
                if (file) {
                    const allowedTypes = ['video/mp4', 'video/webm'];
                    if (!allowedTypes.includes(file.type)) {
                        alert("Chỉ chấp nhận file video định dạng MP4 hoặc WebM.");
                        $(this).val("");
                        return;
                    }
                    if (file.size > 25 * 1024 * 1024) {
                        alert("Dung lượng video vượt quá 25MB.");
                        $(this).val("");
                        return;
                    }
                    selectedPostVideo = file;
                    const videoUrl = URL.createObjectURL(file);
                    $("#postVideoPreview").attr("src", videoUrl);
                    $("#postVideoPreviewWrapper").removeClass("d-none");
                }
            });

            $("#btnRemoveVideoPreview").click(function () {
                selectedPostVideo = null;
                $("#postVideoInput").val("");
                $("#postVideoPreview").attr("src", "");
                $("#postVideoPreviewWrapper").addClass("d-none");
            });

            // 2. Gửi bài đăng mới bằng AJAX
            $("#createPostForm").submit(function (e) {
                e.preventDefault();
                const content = $("#postContent").val().trim();

                if (!content && selectedPostFiles.length === 0 && !selectedPostVideo) {
                    alert("Vui lòng nhập nội dung, chọn ảnh hoặc chọn video để đăng bài.");
                    return;
                }

                const formData = new FormData();
                formData.append("content", content);
                selectedPostFiles.forEach(file => {
                    formData.append("images", file);
                });
                if (selectedPostVideo) {
                    formData.append("video", selectedPostVideo);
                }

                $.ajax({
                    url: "/Post/Create",
                    type: "POST",
                    data: formData,
                    contentType: false,
                    processData: false,
                    success: function (res) {
                        if (res.success) {
                            $("#postContent").val("");
                            $("#postImageInput").val("");
                            $("#postVideoInput").val("");
                            selectedPostFiles = [];
                            selectedPostVideo = null;
                            $("#postImagePreviewContainer").empty();
                            $("#postImagePreviewWrapper").addClass("d-none");
                            $("#postVideoPreview").attr("src", "");
                            $("#postVideoPreviewWrapper").addClass("d-none");
                            $("#postValidationAlert").addClass("d-none");
                        } else {
                            if (res.isViolent) {
                                $("#postValidationMessage").text(res.message);
                                $("#postValidationAlert").removeClass("d-none");
                            } else {
                                alert(res.message);
                            }
                        }
                    },
                    error: function () {
                        alert("Có lỗi xảy ra khi gửi bài viết.");
                    }
                });
            });


            // 4. Gửi yêu cầu kết bạn bằng AJAX
            $(document).on("click", ".btn-add-friend", function () {
                const userId = $(this).data("user-id");
                const $btn = $(this);
                $.post("/Friend/SendRequest", { receiverId: userId }, function (res) {
                    if (res.success) {
                        $btn.replaceWith(`<button class="btn btn-sm btn-outline-secondary" disabled><i class="bi bi-clock-history me-1"></i>Đã gửi</button>`);
                    } else {
                        alert(res.message);
                    }
                });
            });

            // Gửi cảm xúc lên server
            function sendReaction(postId, reactionType) {
                $.post("/Post/ToggleLike", { postId: postId, reactionType: reactionType }, function (res) {
                    if (res.success) {
                        updateLikeButtonUI(postId, res.isLiked, res.reactionType);
                    } else {
                        alert(res.message);
                    }
                });
            }

            // Cập nhật giao diện nút Thích cục bộ
            function updateLikeButtonUI(postId, isLiked, reactionType) {
                const btn = $(`.btn-toggle-like[data-post-id="${postId}"]`);
                if (btn.length === 0) return;
                
                btn.removeClass("text-muted text-primary text-danger text-warning");
                
                if (isLiked) {
                    btn.attr("data-current-reaction", reactionType);
                    
                    let btnClass = "text-muted";
                    let btnIcon = "bi-hand-thumbs-up";
                    let btnText = "Thích";
                    
                    switch (reactionType) {
                        case "Like":
                            btnClass = "text-primary";
                            btnIcon = "bi-hand-thumbs-up-fill";
                            btnText = "Thích";
                            break;
                        case "Love":
                            btnClass = "text-danger";
                            btnIcon = "bi-heart-fill";
                            btnText = "Yêu thích";
                            break;
                        case "Haha":
                            btnClass = "text-warning";
                            btnIcon = "bi-emoji-laughing-fill";
                            btnText = "Haha";
                            break;
                        case "Wow":
                            btnClass = "text-warning";
                            btnIcon = "bi-emoji-surprise-fill";
                            btnText = "Wow";
                            break;
                        case "Sad":
                            btnClass = "text-warning";
                            btnIcon = "bi-emoji-frown-fill";
                            btnText = "Buồn";
                            break;
                        case "Angry":
                            btnClass = "text-danger";
                            btnIcon = "bi-emoji-angry-fill";
                            btnText = "Phẫn nộ";
                            break;
                    }
                    
                    btn.addClass(btnClass);
                    btn.find("i").attr("class", `bi ${btnIcon} me-1`);
                    btn.find("span").text(btnText);
                } else {
                    btn.attr("data-current-reaction", "");
                    btn.addClass("text-muted");
                    btn.find("i").attr("class", "bi bi-hand-thumbs-up me-1");
                    btn.find("span").text("Thích");
                }
            }

            // Click trực tiếp nút Thích (Toggle mặc định / Hủy)
            $(document).on("click", ".btn-toggle-like", function () {
                const postId = $(this).data("post-id");
                const currentReaction = $(this).attr("data-current-reaction");
                const reactionToToggle = currentReaction ? currentReaction : "Like";
                
                sendReaction(postId, reactionToToggle);
            });

            // Chọn cảm xúc cụ thể từ bảng Emoji
            $(document).on("click", ".reaction-emoji-item", function (e) {
                e.stopPropagation();
                const postId = $(this).data("post-id");
                const reaction = $(this).data("reaction");
                
                sendReaction(postId, reaction);
            });

            // Click vào thống kê để xem ai đã thả cảm xúc
            $(document).on("click", ".btn-view-reactions", function () {
                const postId = $(this).data("post-id");
                
                $("#reactionsModalBody").html('<div class="text-center py-4"><span class="spinner-border spinner-border-sm text-muted"></span></div>');
                $("#reactionsDetailModal").modal("show");
                
                $.get("/Post/GetPostReactions", { postId: postId }, function (res) {
                    if (res.success) {
                        $("#reactionsModalBody").empty();
                        
                        if (res.reactions.length === 0) {
                            $("#reactionsModalBody").html('<div class="text-center text-muted small py-3">Chưa có ai bày tỏ cảm xúc.</div>');
                            return;
                        }
                        
                        const emojiMap = {
                            "Like": "👍",
                            "Love": "❤️",
                            "Haha": "😆",
                            "Wow": "😮",
                            "Sad": "😢",
                            "Angry": "😡"
                        };
                        
                        res.reactions.forEach(function (item) {
                            const emoji = emojiMap[item.reactionType] || "👍";
                            const rowHtml = `
                                <div class="d-flex align-items-center justify-content-between py-2 border-bottom border-light">
                                    <div class="d-flex align-items-center">
                                        <img src="${item.avatarUrl}" alt="${item.fullName}" class="avatar avatar-sm me-2.5" />
                                        <div>
                                            <h6 class="fw-semibold mb-0 small" style="font-size: 0.85rem;">${item.fullName}</h6>
                                            <span class="text-muted small" style="font-size: 0.7rem;">@${item.username}</span>
                                        </div>
                                    </div>
                                    <span style="font-size: 1.35rem;">${emoji}</span>
                                </div>
                            `;
                            $("#reactionsModalBody").append(rowHtml);
                        });
                    } else {
                        $("#reactionsModalBody").html(`<div class="text-center text-danger small py-3">${res.message}</div>`);
                    }
                });
            });

            // Gửi yêu cầu xóa bài đăng qua AJAX
            $(document).on("click", ".btn-delete-post-trigger", function (e) {
                e.preventDefault();
                const postId = $(this).data("post-id");
                
                if (confirm("Bạn có chắc chắn muốn xóa bài viết này không? Hành động này không thể hoàn tác.")) {
                    $.post("/Post/Delete", { id: postId }, function (res) {
                        if (res.success) {
                            // Sẽ tự động xóa qua SignalR ReceiveDeletedPost
                        } else {
                            alert(res.message);
                        }
                    });
                }
            });

            // Nhận sự kiện xóa bài viết thời gian thực
            connection.on("ReceiveDeletedPost", function (postId, authorUserId) {
                const card = $(`#postCard-${postId}`);
                if (card.length > 0) {
                    card.fadeOut(350, function () {
                        $(this).remove();
                    });
                }

                const currentUserId = window.currentUserId || $("#currentUserIdVal").val();
                if (authorUserId && currentUserId && authorUserId.toString() === currentUserId.toString()) {
                    const countBadge = $('.user-post-count-badge');
                    if (countBadge.length > 0) {
                        let count = parseInt(countBadge.first().text()) || 0;
                        if (count > 0) {
                            countBadge.text(count - 1);
                        }
                    }
                }
            });

            // 6. Kích hoạt Modal Báo cáo bài viết
            $(document).on("click", ".btn-report-post-trigger", function (e) {
                e.preventDefault();
                const postId = $(this).attr("data-post-id") || $(this).data("post-id");
                if (!postId) {
                    alert("Không tìm thấy mã bài viết!");
                    return;
                }
                $("#reportPostId").val(postId);
                $("#reportReason").val("");
                $("#otherReason").val("");
                $("#otherReasonWrapper").addClass("d-none");
                $("#otherReason").prop("required", false);
                $("#reportAlertBox").addClass("d-none").text("");

                const modalEl = document.getElementById("reportPostModal");
                if (modalEl && typeof bootstrap !== "undefined" && bootstrap.Modal) {
                    const modal = bootstrap.Modal.getOrCreateInstance(modalEl);
                    modal.show();
                } else {
                    $("#reportPostModal, #reportModal").modal("show");
                }
            });

            // 7. Ẩn/Hiện lý do khác
            $(document).on("change", "#reportReason", function () {
                if ($(this).val() === "Lý do khác") {
                    $("#otherReasonWrapper").removeClass("d-none");
                    $("#otherReason").prop("required", true).focus();
                } else {
                    $("#otherReasonWrapper").addClass("d-none");
                    $("#otherReason").prop("required", false);
                }
            });

            // 8. Submit Báo cáo bài viết qua AJAX
            $(document).on("submit", "#reportPostForm", function (e) {
                e.preventDefault();
                const postId = $("#reportPostId").val();
                let reason = $("#reportReason").val();
                if (!reason) {
                    $("#reportAlertBox").removeClass("d-none").text("Vui lòng chọn lý do báo cáo vi phạm.");
                    return;
                }
                if (reason === "Lý do khác") {
                    reason = $("#otherReason").val()?.trim();
                    if (!reason) {
                        $("#reportAlertBox").removeClass("d-none").text("Vui lòng nhập chi tiết lý do vi phạm.");
                        $("#otherReason").focus();
                        return;
                    }
                }

                $("#reportAlertBox").addClass("d-none").text("");
                const $btnSubmit = $(this).find("button[type='submit']");
                const originalHtml = $btnSubmit.html();
                $btnSubmit.prop("disabled", true).html('<span class="spinner-border spinner-border-sm me-1" role="status" aria-hidden="true"></span>Đang gửi...');

                $.ajax({
                    url: "/Report/Create",
                    type: "POST",
                    data: { postId: postId, reason: reason },
                    success: function (res) {
                        $btnSubmit.prop("disabled", false).html(originalHtml);
                        const modalEl = document.getElementById("reportPostModal");
                        if (modalEl && typeof bootstrap !== "undefined" && bootstrap.Modal) {
                            const modal = bootstrap.Modal.getInstance(modalEl);
                            if (modal) modal.hide();
                        }
                        $("#reportPostModal, #reportModal").modal("hide");

                        if (res.success) {
                            alert(res.message);
                        } else {
                            alert(res.message);
                        }
                    },
                    error: function (xhr, status, error) {
                        $btnSubmit.prop("disabled", false).html(originalHtml);
                        alert("Lỗi máy chủ khi gửi báo cáo: " + (xhr.responseJSON?.message || error || "Vui lòng thử lại"));
                    }
                });
            });

            function renderSingleChatMessage(msg, isOwn, friendAvatar) {
                let bodyHtml = '';
                
                // 1. Text content
                if (msg.content) {
                    bodyHtml += `<div>${msg.content}</div>`;
                }

                // 2. Chat image
                if (msg.imageUrl) {
                    bodyHtml += `<div class="mt-1"><img src="${msg.imageUrl}" class="img-fluid rounded border shadow-sm cursor-pointer" style="max-height: 180px; width: 100%; object-fit: cover;" onclick="window.open('${msg.imageUrl}')" /></div>`;
                }

                // 3. Shared post card
                if (msg.sharedPost) {
                    const postContent = msg.sharedPost.content ? msg.sharedPost.content.replace(/"/g, '&quot;') : '';
                    bodyHtml += `
                        <div class="card border border-primary-subtle shadow-sm my-1 text-dark text-start overflow-hidden rounded-3 cursor-pointer" onclick="scrollToPost(${msg.sharedPost.postId})" style="background-color: #f8f9fa;">
                            <div class="card-body p-2">
                                <div class="d-flex align-items-center mb-1">
                                    <img src="${msg.sharedPost.authorAvatar || '/images/default-avatar.png'}" class="avatar avatar-xs rounded-circle me-1" style="width: 22px; height: 22px;" />
                                    <span class="fw-bold extra-small text-dark me-1">${msg.sharedPost.authorName}</span>
                                    <span class="badge bg-secondary-subtle text-secondary extra-small" style="font-size: 0.65rem;">Bài viết</span>
                                </div>
                                ${postContent ? `<p class="mb-1 text-secondary small text-truncate" style="max-height: 40px; font-size: 0.78rem;">${postContent}</p>` : ''}
                                ${msg.sharedPost.imageUrl ? `<img src="${msg.sharedPost.imageUrl}" class="img-fluid rounded mt-1" style="max-height: 110px; width: 100%; object-fit: cover;" />` : ''}
                            </div>
                        </div>
                    `;
                }

                if (isOwn) {
                    return `
                        <div class="d-flex align-items-start justify-content-end">
                            <div class="p-2 bg-primary text-white rounded shadow-sm small" style="max-width: 80%; word-break: break-word;">
                                ${bodyHtml}
                            </div>
                        </div>
                    `;
                } else {
                    return `
                        <div class="d-flex align-items-start gap-2">
                            <img src="${friendAvatar}" class="avatar avatar-sm rounded-circle" style="width: 24px; height: 24px;" />
                            <div class="p-2 bg-white rounded shadow-sm text-dark small" style="max-width: 80%; word-break: break-word;">
                                ${bodyHtml}
                            </div>
                        </div>
                    `;
                }
            }

            // 9. Nhận tin nhắn trực tiếp qua SignalR
            connection.on("ReceiveDirectMessage", function (message) {
                const currentUserId = window.currentUserId || $("#currentUserIdVal").val();
                const activeFriendId = $("#chatFriendId").val();
                
                // Nếu tin nhắn là giữa người dùng hiện tại và người bạn đang mở chat
                if ((message.senderId == currentUserId && message.receiverId == activeFriendId) ||
                    (message.senderId == activeFriendId && message.receiverId == currentUserId)) {
                    
                    const isOwn = message.senderId == currentUserId;
                    const friendAvatar = $("#chatFriendAvatar").attr('src');
                    const messageHtml = renderSingleChatMessage(message, isOwn, friendAvatar);
                    
                    $("#chatMessageList").append(messageHtml);
                    scrollChatToBottom();
                } else if (message.receiverId == currentUserId) {
                    // Nếu là tin nhắn gửi đến người dùng hiện tại nhưng chưa mở chat
                    const friendRow = $(`.btn-open-chat[data-friend-id="${message.senderId}"]`);
                    if (friendRow.length > 0) {
                        friendRow.find(".btn-light").addClass("bg-danger-subtle");
                        friendRow.find(".bi-chat-fill").removeClass("text-primary").addClass("text-danger");
                    }
                }
            });

            // 10. Xử lý xem trước ảnh chọn đính kèm chat
            $("#chatImageInput").change(function () {
                const file = this.files[0];
                if (file) {
                    const reader = new FileReader();
                    reader.onload = function (e) {
                        $("#chatImagePreview").attr("src", e.target.result);
                        $("#chatImagePreviewContainer").removeClass("d-none");
                    };
                    reader.readAsDataURL(file);
                }
            });

            $("#btnRemoveChatImage").click(function () {
                $("#chatImageInput").val("");
                $("#chatImagePreview").attr("src", "");
                $("#chatImagePreviewContainer").addClass("d-none");
            });

            // 11. Mở hộp chat khi click vào bạn bè
            $(document).on("click", ".btn-open-chat", function (e) {
                e.preventDefault();
                const target = $(this).closest(".btn-open-chat");
                const friendId = target.data("friend-id") || target.attr("data-friend-id");
                const friendName = target.data("friend-name") || target.attr("data-friend-name") || "Bạn bè";
                const friendAvatar = target.data("friend-avatar") || target.attr("data-friend-avatar") || "/images/default-avatar.png";
                
                if (!friendId) return;

                // Khôi phục icon chat bình thường
                target.find(".btn-light").removeClass("bg-danger-subtle");
                target.find(".bi-chat-fill").removeClass("text-danger").addClass("text-primary");

                $("#chatFriendId").val(friendId);
                $("#chatFriendName").text(friendName);
                $("#chatFriendAvatar").attr("src", friendAvatar);
                
                $("#chatMessageList").html('<div class="text-center py-4"><span class="spinner-border spinner-border-sm text-primary"></span><div class="text-muted small mt-2">Đang tải cuộc trò chuyện...</div></div>');
                $("#chatBox").removeClass("d-none");
                
                // Mở rộng hộp chat
                const collapseEl = document.getElementById('chatBoxCollapse');
                if (collapseEl && typeof bootstrap !== 'undefined') {
                    const collapse = bootstrap.Collapse.getOrCreateInstance(collapseEl, { toggle: false });
                    collapse.show();
                } else {
                    $("#chatBoxCollapse").addClass("show");
                }
                $("#minimizeChatIcon").removeClass("bi-plus-lg").addClass("bi-dash-lg");
                setTimeout(() => $("#chatInput").focus(), 200);

                // Tải lịch sử tin nhắn
                $.get("/Chat/GetMessages", { friendId: friendId }, function (res) {
                    if (res.success) {
                        $("#chatMessageList").empty();
                        const currentUserId = window.currentUserId || $("#currentUserIdVal").val();
                        
                        if (!res.messages || res.messages.length === 0) {
                            $("#chatMessageList").html('<div class="text-center text-muted small py-4"><i class="bi bi-chat-heart fs-3 d-block mb-1 text-primary-emphasis"></i>Bắt đầu trò chuyện bằng cách gửi tin nhắn!</div>');
                        } else {
                            res.messages.forEach(function (msg) {
                                const isOwn = msg.senderId == currentUserId;
                                const msgHtml = renderSingleChatMessage(msg, isOwn, friendAvatar);
                                $("#chatMessageList").append(msgHtml);
                            });
                            scrollChatToBottom();
                        }
                    } else {
                        $("#chatMessageList").html(`<div class="text-center text-danger small py-4"><i class="bi bi-exclamation-circle me-1"></i>${res.message}</div>`);
                    }
                }).fail(function () {
                    $("#chatMessageList").html('<div class="text-center text-danger small py-4"><i class="bi bi-wifi-off me-1"></i>Không thể tải tin nhắn. Vui lòng kiểm tra lại kết nối.</div>');
                });
            });

            // 12. Gửi tin nhắn qua form chat (Hỗ trợ Văn bản, Hình ảnh & SignalR)
            $("#chatForm").submit(function (e) {
                e.preventDefault();
                const friendId = $("#chatFriendId").val();
                const content = $("#chatInput").val().trim();
                const imageFileInput = $("#chatImageInput")[0];
                const imageFile = imageFileInput && imageFileInput.files ? imageFileInput.files[0] : null;
                
                if (!content && !imageFile) return;

                const formData = new FormData();
                formData.append("receiverId", friendId);
                if (content) formData.append("content", content);
                if (imageFile) formData.append("image", imageFile);
                
                // Reset form & preview ngay lập tức
                $("#chatInput").val("");
                $("#chatImageInput").val("");
                $("#chatImagePreviewContainer").addClass("d-none");
                
                $.ajax({
                    url: "/Chat/SendMessage",
                    type: "POST",
                    data: formData,
                    contentType: false,
                    processData: false,
                    success: function (res) {
                        if (!res.success) {
                            alert(res.message);
                        }
                    },
                    error: function () {
                        alert("Có lỗi xảy ra khi gửi tin nhắn.");
                    }
                });
            });

            // 13. Mở Modal Chia sẻ bài viết
            $(document).on("click", ".btn-share-post-trigger", function (e) {
                e.preventDefault();
                const postId = $(this).data("post-id");
                const author = $(this).data("post-author");
                const avatar = $(this).data("post-avatar");
                const content = $(this).data("post-content");
                const image = $(this).data("post-image");

                $("#sharePostId").val(postId);
                $("#sharePostAuthorName").text(author);
                $("#sharePostAuthorAvatar").attr("src", avatar);
                $("#sharePostSnippet").text(content || "Hình ảnh bài viết");
                $("#shareCustomMessage").val("");

                if (image) {
                    $("#sharePostImagePreview").attr("src", image);
                    $("#sharePostImageWrapper").removeClass("d-none");
                } else {
                    $("#sharePostImageWrapper").addClass("d-none");
                }

                $("#shareFriendsList").html('<div class="text-center py-3 text-muted small"><span class="spinner-border spinner-border-sm me-1"></span> Đang tải bạn bè...</div>');
                $("#sharePostModal").modal("show");

                // Lấy danh sách bạn bè để chọn người nhận
                $.get("/Chat/GetFriendsForShare", function (res) {
                    if (res.success) {
                        if (res.friends.length === 0) {
                            $("#shareFriendsList").html('<div class="text-center text-muted small py-3">Bạn chưa có bạn bè để chia sẻ.</div>');
                        } else {
                            let friendsHtml = '';
                            res.friends.forEach(function (f) {
                                friendsHtml += `
                                    <div class="d-flex align-items-center justify-content-between p-2 rounded hover-bg-light border-bottom">
                                        <div class="d-flex align-items-center me-2">
                                            <img src="${f.avatarUrl || '/images/default-avatar.png'}" class="avatar avatar-sm rounded-circle me-2" style="width: 32px; height: 32px;" />
                                            <div>
                                                <div class="fw-semibold small text-dark">${f.fullName}</div>
                                                <div class="text-muted extra-small">@${f.username}</div>
                                            </div>
                                        </div>
                                        <button type="button" class="btn btn-primary btn-sm px-3 btn-do-share" data-receiver-id="${f.userId}" style="border-radius: 16px;">
                                            <i class="bi bi-send me-1"></i>Gửi
                                        </button>
                                    </div>
                                `;
                            });
                            $("#shareFriendsList").html(friendsHtml);
                        }
                    } else {
                        $("#shareFriendsList").html(`<div class="text-center text-danger small py-3">${res.message}</div>`);
                    }
                });
            });

            // 14. Bấm Gửi bài viết cho bạn bè trong Modal Chia sẻ
            $(document).on("click", ".btn-do-share", function () {
                const btn = $(this);
                const receiverId = btn.data("receiver-id");
                const postId = $("#sharePostId").val();
                const customMessage = $("#shareCustomMessage").val().trim();

                btn.prop("disabled", true).html('<span class="spinner-border spinner-border-sm"></span>');

                $.post("/Chat/SendMessage", { receiverId: receiverId, content: customMessage, sharedPostId: postId }, function (res) {
                    if (res.success) {
                        btn.removeClass("btn-primary").addClass("btn-success").html('<i class="bi bi-check-lg me-1"></i>Đã gửi');
                    } else {
                        btn.prop("disabled", false).html('<i class="bi bi-send me-1"></i>Gửi');
                        alert(res.message);
                    }
                }).fail(function (xhr) {
                    btn.prop("disabled", false).html('<i class="bi bi-send me-1"></i>Gửi');
                    alert("Có lỗi xảy ra khi gửi tin nhắn. Vui lòng thử lại!");
                });
            });

            // 15. Toggle Save Post (M06)
            $(document).on("click", ".btn-toggle-save-post", function (e) {
                e.preventDefault();
                const btn = $(this);
                const postId = btn.data("post-id");
                
                $.post("/Post/ToggleSavePost", { postId: postId }, function (res) {
                    if (res.success) {
                        if (res.isSaved) {
                            btn.html('<i class="bi bi-bookmark-fill me-2 text-warning"></i><span>Bỏ lưu bài viết</span>');
                        } else {
                            btn.html('<i class="bi bi-bookmark me-2"></i><span>Lưu bài viết</span>');
                        }
                    } else {
                        alert(res.message);
                    }
                });
            });

            // 16. Trigger Edit Post Modal (M05)
            $(document).on("click", ".btn-edit-post-trigger", function (e) {
                e.preventDefault();
                const btn = $(this);
                const postId = btn.data("post-id");
                const content = btn.data("content") || "";
                const hasImage = btn.data("has-image");
                const imageUrl = btn.data("image-url");

                $("#editPostId").val(postId);
                $("#editPostContent").val(content);
                $("#editRemoveImage").prop("checked", false);
                $("#editPostAlert").addClass("d-none");

                if (hasImage && imageUrl) {
                    const firstImg = imageUrl.split(',')[0];
                    $("#editPostImagePreview").attr("src", firstImg);
                    $("#editPostImagePreviewArea").removeClass("d-none");
                } else {
                    $("#editPostImagePreviewArea").addClass("d-none");
                }

                $("#editPostModal").modal("show");
            });

            // 17. Submit Edit Post Form (M05)
            $("#editPostForm").on("submit", function (e) {
                e.preventDefault();
                const postId = $("#editPostId").val();
                const content = $("#editPostContent").val();
                const removeImage = $("#editRemoveImage").is(":checked");

                $("#btnSaveEditPost").prop("disabled", true);
                $("#editPostAlert").addClass("d-none");

                $.post("/Post/EditPost", { postId: postId, content: content, removeImage: removeImage }, function (res) {
                    $("#btnSaveEditPost").prop("disabled", false);
                    if (res.success) {
                        $("#editPostModal").modal("hide");
                        const postCard = $(`#postCard-${postId}`);
                        if (postCard.length > 0) {
                            // Update content
                            let pContent = postCard.find(".card-text").first();
                            if (pContent.length > 0) {
                                pContent.text(res.content);
                            } else if (res.content) {
                                postCard.find(".card-body").append(`<p class="card-text fs-6 mb-3" style="white-space: pre-wrap;">${res.content}</p>`);
                            }
                            
                            // Remove image if requested
                            if (removeImage) {
                                postCard.find(".post-image-container, .post-images-grid").remove();
                                postCard.find(".btn-edit-post-trigger").data("has-image", false);
                            }
                            postCard.find(".btn-edit-post-trigger").data("content", res.content);

                            // Add badge
                            if (postCard.find(".post-updated-badge").length === 0) {
                                postCard.find(".text-muted.small").first().after(' <span class="text-muted small ms-1 post-updated-badge" title="Đã chỉnh sửa">(đã chỉnh sửa)</span>');
                            }
                        }
                    } else {
                        $("#editPostAlertMsg").text(res.message);
                        $("#editPostAlert").removeClass("d-none");
                    }
                }).fail(function (xhr) {
                    $("#btnSaveEditPost").prop("disabled", false);
                    if (xhr.status === 403) {
                        $("#editPostAlertMsg").text("Bạn không có quyền chỉnh sửa bài viết này.");
                    } else {
                        $("#editPostAlertMsg").text("Có lỗi xảy ra khi lưu bài viết. Vui lòng thử lại!");
                    }
                    $("#editPostAlert").removeClass("d-none");
                });
            });

            function scrollChatToBottom() {
                const list = document.getElementById("chatMessageList");
                if (list) {
                    list.scrollTop = list.scrollHeight;
                }
            }

        // Hàm điều khiển chat & cuộn bài viết toàn cục
        function scrollToPost(postId) {
            const postCard = $(`#postCard-${postId}`);
            if (postCard.length > 0) {
                $('html, body').animate({
                    scrollTop: postCard.offset().top - 80
                }, 400);
                postCard.addClass("border-primary shadow-lg");
                setTimeout(() => postCard.removeClass("border-primary shadow-lg"), 2500);
            } else {
                alert("Bài viết không có trong danh sách hiển thị hiện tại.");
            }
        }

        function closeChatBox() {
            $("#chatBox").addClass("d-none");
            $("#chatFriendId").val("");
        }

        function toggleMinimizeChat() {
            const collapseEl = document.getElementById('chatBoxCollapse');
            const collapse = bootstrap.Collapse.getOrCreateInstance(collapseEl);
            collapse.toggle();
            
            const icon = $("#minimizeChatIcon");
            if (icon.hasClass("bi-dash-lg")) {
                icon.removeClass("bi-dash-lg").addClass("bi-plus-lg");
            } else {
                icon.removeClass("bi-plus-lg").addClass("bi-dash-lg");
            }
        }

        $(document).on("click", "#btnMobileQuickChat", function () {
            const card = $("#friendsListCard");
            if (card.length > 0) {
                $('html, body').animate({ scrollTop: card.offset().top - 80 }, 300);
            }
        });

        window.scrollToPost = scrollToPost;
        window.closeChatBox = closeChatBox;
        window.toggleMinimizeChat = toggleMinimizeChat;
    });
    