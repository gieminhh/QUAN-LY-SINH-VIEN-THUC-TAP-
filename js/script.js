/**
 * USMART - Vinh University Portal Interaction Logic
 */

document.addEventListener('DOMContentLoaded', function () {
  const sidebar = document.getElementById('appSidebar');
  const sidebarToggle = document.getElementById('sidebarToggle');
  const sidebarBackdrop = document.getElementById('sidebarBackdrop');
  const sidebarSearchInput = document.getElementById('sidebarSearchInput');
  const menuItems = document.querySelectorAll('.sidebar-menu .menu-item');
  const backHomeBtn = document.getElementById('backHomeBtn');
  const topSearchInput = document.getElementById('topSearchInput');
  const topSearchBtn = document.getElementById('topSearchBtn');

  // 1. Toggle Sidebar (Hamburger Button)
  if (sidebarToggle && sidebar) {
    sidebarToggle.addEventListener('click', function () {
      sidebar.classList.toggle('collapsed');
      if (window.innerWidth <= 768 && sidebarBackdrop) {
        sidebarBackdrop.classList.toggle('show');
      }
    });
  }

  // Close sidebar when clicking backdrop on mobile
  if (sidebarBackdrop) {
    sidebarBackdrop.addEventListener('click', function () {
      sidebar.classList.add('collapsed');
      sidebarBackdrop.classList.remove('show');
    });
  }

  // 2. Expandable Accordion Menu Items
  menuItems.forEach(item => {
    const link = item.querySelector('.menu-link');
    const submenu = item.querySelector('.submenu');

    if (submenu && link) {
      link.addEventListener('click', function (e) {
        e.preventDefault();
        
        // Toggle current item
        const isOpen = item.classList.contains('open');
        item.classList.toggle('open', !isOpen);

        // Optional: close other open items for cleaner accordion
        menuItems.forEach(otherItem => {
          if (otherItem !== item && !otherItem.querySelector('.active')) {
            otherItem.classList.remove('open');
          }
        });
      });
    } else if (link) {
      // Leaf link without submenu
      link.addEventListener('click', function (e) {
        e.preventDefault();
        
        // Remove active class from all links
        document.querySelectorAll('.menu-link, .submenu-link').forEach(l => l.classList.remove('active'));
        link.classList.add('active');

        const title = link.querySelector('.menu-label')?.textContent.trim() || '';
        showToast(`Đã chọn: ${title}`);
      });
    }
  });

  // 3. Submenu Links click handler
  const submenuLinks = document.querySelectorAll('.submenu-link');
  submenuLinks.forEach(subLink => {
    subLink.addEventListener('click', function (e) {
      e.preventDefault();
      document.querySelectorAll('.menu-link, .submenu-link').forEach(l => l.classList.remove('active'));
      subLink.classList.add('active');

      const title = subLink.textContent.trim();
      showToast(`Đang chuyển đến: ${title}`);
    });
  });

  // 4. Live Search Filter in Sidebar
  if (sidebarSearchInput) {
    sidebarSearchInput.addEventListener('input', function () {
      const keyword = removeVietnameseTones(this.value.trim().toLowerCase());

      menuItems.forEach(item => {
        const itemText = removeVietnameseTones(item.textContent.toLowerCase());
        const hasMatch = itemText.includes(keyword);

        if (keyword === '') {
          item.style.display = '';
          // Close submenus on reset unless was active
          if (!item.querySelector('.active')) {
            item.classList.remove('open');
          }
        } else if (hasMatch) {
          item.style.display = '';
          if (item.querySelector('.submenu')) {
            item.classList.add('open');
          }
        } else {
          item.style.display = 'none';
        }
      });
    });
  }

  // 5. "Quay lại trang chủ" Button
  if (backHomeBtn) {
    backHomeBtn.addEventListener('click', function (e) {
      e.preventDefault();
      document.querySelectorAll('.menu-link, .submenu-link').forEach(l => l.classList.remove('active'));
      showToast('Đang trở về Trang chủ Cổng Sinh Viên...');
    });
  }

  // 6. Top Navbar Search
  function handleTopSearch() {
    const query = topSearchInput?.value.trim();
    if (query) {
      showToast(`Đang tìm kiếm từ khóa: "${query}"...`);
    } else {
      topSearchInput?.focus();
    }
  }

  if (topSearchBtn) {
    topSearchBtn.addEventListener('click', handleTopSearch);
  }
  if (topSearchInput) {
    topSearchInput.addEventListener('keypress', function (e) {
      if (e.key === 'Enter') {
        handleTopSearch();
      }
    });
  }

  // 7. Toast Notification Utility
  function showToast(message) {
    const toastEl = document.getElementById('liveToast');
    const toastBody = document.getElementById('toastBody');
    if (toastEl && toastBody && window.bootstrap) {
      toastBody.textContent = message;
      const toast = new bootstrap.Toast(toastEl, { delay: 2500 });
      toast.show();
    }
  }

  // Helper: Remove Vietnamese tones for accent-insensitive search
  function removeVietnameseTones(str) {
    str = str.replace(/à|á|ạ|ả|ã|â|ầ|ấ|ậ|ẩ|ẫ|ă|ằ|ắ|ặ|ẳ|ẵ/g, 'a');
    str = str.replace(/è|é|ẹ|ẻ|ẽ|ê|ề|ế|ệ|ể|ễ/g, 'e');
    str = str.replace(/ì|í|ị|ỉ|ĩ/g, 'i');
    str = str.replace(/ò|ó|ọ|ỏ|õ|ô|ồ|ố|ộ|ổ|ỗ|ơ|ờ|ớ|ợ|ở|ỡ/g, 'o');
    str = str.replace(/ù|ú|ụ|ủ|ũ|ư|ừ|ứ|ự|ử|ữ/g, 'u');
    str = str.replace(/ỳ|ý|ỵ|ỷ|ỹ/g, 'y');
    str = str.replace(/đ/g, 'd');
    str = str.replace(/À|Á|Ạ|Ả|Ã|Â|Ầ|Ấ|Ậ|Ẩ|Ẫ|Ă|Ằ|Ắ|Ặ|Ẳ|Ẵ/g, 'A');
    str = str.replace(/È|É|Ẹ|Ẻ|Ẽ|Ê|Ề|Ế|Ệ|Ể|Ễ/g, 'E');
    str = str.replace(/Ì|Í|Ị|Ỉ|Ĩ/g, 'I');
    str = str.replace(/Ò|Ó|Ọ|Ỏ|Õ|Ô|Ồ|Ố|Ộ|Ổ|Ỗ|Ơ|Ờ|Ớ|Ợ|Ở|Ỡ/g, 'O');
    str = str.replace(/Ù|Ú|Ụ|Ủ|Ũ|Ư|Ừ|Ứ|Ự|Ử|Ữ/g, 'U');
    str = str.replace(/Ỳ|Ý|Ỵ|Ỷ|Ỹ/g, 'Y');
    str = str.replace(/Đ/g, 'D');
    return str;
  }
});
