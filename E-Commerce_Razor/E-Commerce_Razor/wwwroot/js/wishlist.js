console.log("wishlist js loaded");
document.addEventListener("DOMContentLoaded", async function () {
    try {
        const res = await fetch('/Wishlist?handler=Count');
        const data = await res.json();

        // Nếu API trả thẳng số → data là number
        // Nếu API trả object → data.count
        const count = typeof data === "number"
            ? data
            : data?.count ?? 0;

        const badge = document.getElementById('wishlistBadge');

        if (badge) {
            badge.textContent = count;
            badge.style.display = count > 0 ? 'block' : 'none';
        }
    }
    catch (err) {
        console.log("Load wishlist count error:", err);
    }
});