document.addEventListener('DOMContentLoaded', () => {
  const sidebar = document.getElementById('appSidebar');
  const toggle = document.getElementById('sidebarToggle');
  const backdrop = document.getElementById('sidebarBackdrop');
  const sidebarSearch = document.getElementById('sidebarSearchInput');
  const menuItems = [...document.querySelectorAll('.sidebar-menu > .menu-item')];
  const mobile = window.matchMedia('(max-width: 768px)');

  function setSidebar(open) {
    sidebar?.classList.toggle('collapsed', !open);
    backdrop?.classList.toggle('show', mobile.matches && open);
    toggle?.setAttribute('aria-expanded', String(open));
    sidebar?.setAttribute('aria-hidden', String(!open));
    if (sidebar) sidebar.inert = !open;
  }
  setSidebar(!mobile.matches);
  toggle?.addEventListener('click', () => setSidebar(sidebar.classList.contains('collapsed')));
  backdrop?.addEventListener('click', () => setSidebar(false));
  mobile.addEventListener('change', () => setSidebar(!mobile.matches));
  document.addEventListener('keydown', event => {
    if (event.key === 'Escape' && mobile.matches) {
      setSidebar(false);
      toggle?.focus();
    }
  });

  function expand(item, open) {
    item.classList.toggle('open', open);
    item.querySelector('.menu-link')?.setAttribute('aria-expanded', String(open));
  }
  menuItems.forEach(item => {
    const link = item.querySelector('.menu-link');
    if (!item.querySelector('.submenu')) return;
    link.setAttribute('aria-expanded', String(item.classList.contains('open')));
    link.addEventListener('click', event => {
      event.preventDefault();
      expand(item, !item.classList.contains('open'));
    });
  });
  // Real links retain their native navigation and keyboard behavior.
  sidebarSearch?.addEventListener('input', () => {
    const normalize = value => value.toLowerCase().normalize('NFD').replace(/[\u0300-\u036f]/g, '').replace(/đ/g, 'd');
    const keyword = normalize(sidebarSearch.value.trim());
    menuItems.forEach(item => {
      item.hidden = !normalize(item.textContent).includes(keyword);
      if (item.querySelector('.submenu')) {
        expand(item, keyword ? !item.hidden : Boolean(item.querySelector('.active')));
      }
    });
    const empty = document.getElementById('sidebarSearchEmpty');
    if (empty) empty.hidden = menuItems.some(item => !item.hidden);
  });
  // Keep long selected labels readable on narrow screens.
  document.querySelectorAll('[data-selected-label-for]').forEach(label => {
    const select = document.getElementById(label.dataset.selectedLabelFor);
    if (!select) return;
    const update = () => { label.textContent = select.value ? select.selectedOptions[0]?.textContent : ''; };
    select.addEventListener('change', update);
    update();
  });
});
