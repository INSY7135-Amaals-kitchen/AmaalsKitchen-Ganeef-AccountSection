// Please add this JavaScript to your existing site.js file

// Add Amaals Kitchen functionality to existing site.js
document.addEventListener('DOMContentLoaded', function () {
    initializeAmaalsFeatures();
});

function initializeAmaalsFeatures() {
    initializeMenuPage();
    initializeHomePage();
    initializeGeneralFeatures();
}

// Menu page functionality
function initializeMenuPage() {
    // Category filtering
    $(document).on('click', '.category-btn', function () {
        var category = $(this).data('category');
        filterByCategory(category);
    });

    // Enhanced card hover effects
    $(document).on('mouseenter', '.menu-item-card', function () {
        $(this).addClass('shadow-lg');
    }).on('mouseleave', '.menu-item-card', function () {
        $(this).removeClass('shadow-lg');
    });
}

// Enhanced category filter function
function filterByCategory(category) {
    if (category === 'all') {
        $('.menu-item').fadeIn(300);
    } else {
        $('.menu-item').fadeOut(200);
        $('.menu-item[data-category="' + category + '"]').fadeIn(300);
    }

    // Update active button
    $('.category-btn').removeClass('active');
    $('.category-btn[data-category="' + category + '"]').addClass('active');
}

// Cart functionality
// Update your existing addToCart function in site.js
// In your site.js file - update these functions
function addToCart(id, name, price, imageUrl = '') {
    console.log('Adding to cart:', { id, name, price, imageUrl });

    // Make AJAX call to add item to server-side cart
    $.post('/Orders/AddToCart', {
        id: id,
        name: name,
        price: price,
        imageUrl: imageUrl
    }, function (response) {
        if (response.success) {
            // Update cart counter using your existing method
            updateCartCounter(response.totalItems);

            // Show notification
            showNotification('success', name + ' added to cart!', 'fas fa-check-circle');
        }
    }).fail(function (xhr, status, error) {
        console.error('Error adding to cart:', error);
        // Fallback to client-side only if server call fails
        updateCartCounter();
        showNotification('success', name + ' added to cart!', 'fas fa-check-circle');
    });
}

// Update cart counter
function updateCartCounter(count) {
    if (count !== undefined) {
        $('.badge').text(count);
    } else {
        var currentCount = parseInt($('.badge').text()) || 0;
        $('.badge').text(currentCount + 1);
    }
}

// Generic notification system
function showNotification(type, message, icon) {
    const alertClass = `alert-${type}`;
    const notification = $(`<div class="alert ${alertClass} alert-dismissible fade show position-fixed" style="top: 90px; right: 20px; z-index: 9999; min-width: 300px;">
        <i class="${icon} me-2"></i>${message}
        <button type="button" class="btn-close" data-bs-dismiss="alert"></button>
    </div>`);

    $('body').append(notification);

    setTimeout(function () {
        notification.alert('close');
    }, 3000);
}

// Home page functionality
function initializeHomePage() {
    // Add parallax effect to logo using modern event handling
    $(document).on('mousemove', function (e) {
        const logo = $('.logo-circle');
        if (logo.length > 0) {
            const rect = logo[0].getBoundingClientRect();
            const centerX = rect.left + rect.width / 2;
            const centerY = rect.top + rect.height / 2;

            const deltaX = (e.clientX - centerX) * 0.02;
            const deltaY = (e.clientY - centerY) * 0.02;

            logo.css('transform', `translate(${deltaX}px, ${deltaY}px)`);
        }
    });
}
// wwwroot/js/checkout.js - Checkout Page Functionality

$(document).ready(function () {
    console.log('Checkout page loaded');

    // Calculate and display estimated prep time based on items
    function updatePrepTimeEstimate() {
        var itemCount = parseInt($('.summary-line:first span:first').text().match(/\d+/)[0]);
        var baseTime = 15;
        var additionalTime = Math.max(0, (itemCount - 1) * 5);
        var totalTime = Math.min(baseTime + additionalTime, 45);
        var minTime = totalTime - 5;
        var maxTime = totalTime + 5;

        $('#prep-time').text(minTime + '-' + maxTime);
    }

    updatePrepTimeEstimate();

    // Quantity controls - Plus button
    $(document).on('click', '.quantity-btn.plus', function () {
        const itemId = $(this).data('item-id');
        updateQuantity(itemId, 1);
    });

    // Quantity controls - Minus button
    $(document).on('click', '.quantity-btn.minus', function () {
        const itemId = $(this).data('item-id');
        updateQuantity(itemId, -1);
    });

    // Remove item button
    $(document).on('click', '.remove-item-btn', function () {
        const itemId = $(this).data('item-id');
        removeItem(itemId);
    });

    // Clear cart button
    $('#clear-cart-btn').on('click', function () {
        if (confirm('Are you sure you want to clear your cart?')) {
            clearCart();
        }
    });

    // Place order button
    $('#place-order-btn').on('click', function () {
        placeOrder();
    });

    // Update quantity function
    function updateQuantity(itemId, change) {
        const currentQuantity = parseInt($(`.cart-item-card[data-item-id="${itemId}"] .quantity-display`).text());
        const newQuantity = currentQuantity + change;

        if (newQuantity < 1) return;

        $.post('/Orders/UpdateQuantity', {
            itemId: itemId,
            quantity: newQuantity
        }, function (response) {
            if (response.success) {
                $(`.cart-item-card[data-item-id="${itemId}"] .quantity-display`).text(newQuantity);
                const price = parseFloat($(`.cart-item-card[data-item-id="${itemId}"] .cart-item-price`).text().replace('R', ''));
                $(`.cart-item-card[data-item-id="${itemId}"] .item-total`).text('R' + (price * newQuantity).toFixed(2));
                updateTotals(response);
                updatePrepTimeEstimate();
                showNotification('success', 'Cart updated successfully!', 'fas fa-check-circle');
            }
        }).fail(function () {
            showNotification('error', 'Error updating cart', 'fas fa-exclamation-circle');
        });
    }

    // Remove item function
    function removeItem(itemId) {
        if (confirm('Remove this item from your cart?')) {
            $.post('/Orders/RemoveItem', { itemId: itemId }, function (response) {
                if (response.success) {
                    $(`.cart-item-card[data-item-id="${itemId}"]`).fadeOut(300, function () {
                        $(this).remove();
                        if ($('.cart-item-card').length === 0) {
                            location.reload();
                        }
                    });
                    updateTotals(response);
                    updatePrepTimeEstimate();
                    showNotification('success', 'Item removed from cart', 'fas fa-check-circle');
                }
            }).fail(function () {
                showNotification('error', 'Error removing item', 'fas fa-exclamation-circle');
            });
        }
    }

    // Clear cart function
    function clearCart() {
        $.post('/Orders/ClearCart', function () {
            location.reload();
        }).fail(function () {
            showNotification('error', 'Error clearing cart', 'fas fa-exclamation-circle');
        });
    }

    // Place order function
    function placeOrder() {
        $('#place-order-btn').prop('disabled', true).html('<i class="fas fa-spinner fa-spin me-2"></i>Placing Order...');

        var notes = $('#order-notes').val();

        $.post('/Orders/PlaceOrder', { notes: notes }, function (response) {
            if (response.success) {
                $('#orderIdDisplay').text(response.orderId);
                $('#modal-pickup-time').text(response.estimatedPickupTime);
                $('#modal-prep-time').text(response.preparationTime);
                $('#orderSuccessModal').modal('show');
            } else {
                showNotification('error', response.message, 'fas fa-exclamation-circle');
                $('#place-order-btn').prop('disabled', false).html('<i class="fas fa-check-circle me-2"></i>Place Order');
            }
        }).fail(function () {
            showNotification('error', 'Error placing order. Please try again.', 'fas fa-exclamation-circle');
            $('#place-order-btn').prop('disabled', false).html('<i class="fas fa-check-circle me-2"></i>Place Order');
        });
    }

    // Update totals display
    function updateTotals(response) {
        $('#summary-subtotal').text('R' + response.subtotal.toFixed(2));
        $('#summary-tax').text('R' + response.tax.toFixed(2));
        $('#summary-total').text('R' + response.total.toFixed(2));
        $('.summary-line:contains("Items") span:first').text('Items (' + response.totalItems + '):');
        $('.badge').text(response.totalItems);
    }

    // Show notification helper
    function showNotification(type, message, icon) {
        const alertClass = `alert-${type}`;
        const notification = $(`<div class="alert ${alertClass} alert-dismissible fade show position-fixed" style="top: 90px; right: 20px; z-index: 9999; min-width: 300px;">
            <i class="${icon} me-2"></i>${message}
            <button type="button" class="btn-close" data-bs-dismiss="alert"></button>
        </div>`);
        $('body').append(notification);
        setTimeout(function () {
            notification.alert('close');
        }, 3000);
    }
});
// General features
function initializeGeneralFeatures() {
    // Auto-hide alerts after 5 seconds
    setTimeout(function () {
        $('.alert').fadeOut();
    }, 5000);

    // Add loading animation to buttons when clicked using modern event delegation
    $(document).on('click', '.btn', function () {
        var btn = $(this);
        if (!btn.hasClass('no-loading')) {
            btn.prop('disabled', true);
            var originalText = btn.html();
            btn.html('<span class="loading me-2"></span>Loading...');

            setTimeout(function () {
                btn.prop('disabled', false);
                btn.html(originalText);
            }, 1500);
        }
    });
}

// Utility functions for future use
function searchMenu(query) {
    const items = $('.menu-item');

    if (!query.trim()) {
        items.show();
        return;
    }

    items.each(function () {
        const itemName = $(this).find('.item-name').text().toLowerCase();
        const itemDescription = $(this).find('.item-description').text().toLowerCase();

        if (itemName.includes(query.toLowerCase()) || itemDescription.includes(query.toLowerCase())) {
            $(this).show();
        } else {
            $(this).hide();
        }
    });
}

function validateEmail(email) {
    var re = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
    return re.test(email);
}

function formatPrice(price) {
    return 'R' + parseFloat(price).toFixed(2);
}

function updateStatus(orderId, newStatus) {
    $.ajax({
        url: '/Orders/UpdateOrderStatus',
        type: 'POST',
        data: {
            orderId: orderId,
            newStatus: newStatus,
            __RequestVerificationToken: $('input[name="__RequestVerificationToken"]').val()
        },
        success: function (response) {
            if (response.success) {
                // Refresh to show the updated status in the table or timeline
                location.reload();
            } else {
                alert(response.message);
            }
        },
        error: function () {
            alert('Error updating order status.');
        }
    });
}

//function for filter dates in all orders view 
        function onFilterChange() {
            const filterType = document.getElementById('filterType').value;
        const dateContainer = document.getElementById('specificDateContainer');
        dateContainer.style.display = (filterType === 'specific') ? 'block' : 'none';
        }
    

