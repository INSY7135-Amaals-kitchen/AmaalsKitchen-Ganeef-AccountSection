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
function addToCart(id, name, price) {
    // This would integrate with your cart system
    console.log('Added to cart:', { id, name, price });

    // Update cart counter
    updateCartCounter();

    // Show notification
    showNotification('success', name + ' added to cart!', 'fas fa-check-circle');
}

// Update cart counter
function updateCartCounter() {
    var currentCount = parseInt($('.badge').text()) || 0;
    $('.badge').text(currentCount + 1);
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