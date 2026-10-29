(function (global) {
  var SB = global.SB = global.SB || {};
  var A = SB.A = { views: {}, state: {}, actions: {} };

  A.dom = {
    loginScreen: document.getElementById('loginScreen'),
    loginForm: document.getElementById('loginForm'),
    loginBtn: document.getElementById('loginBtn'),
    shell: document.getElementById('appShell'),
    side: document.getElementById('side'),
    sideScrim: document.getElementById('sideScrim'),
    burger: document.getElementById('burger'),
    navLinks: Array.prototype.slice.call(document.querySelectorAll('.side-link[data-view]')),
    viewTitle: document.getElementById('viewTitle'),
    viewSub: document.getElementById('viewSub'),
    viewActions: document.getElementById('viewActions'),
    viewRoot: document.getElementById('viewRoot'),
    userName: document.getElementById('userName'),
    userRole: document.getElementById('userRole'),
    userInitial: document.getElementById('userInitial'),
    badge: document.getElementById('openOrdersBadge'),
    logoutBtn: document.getElementById('logoutBtn'),
    liveChip: document.getElementById('liveChip'),
    liveChipText: document.getElementById('liveChipText')
  };

  A.me = null;
  A.onReload = null;

  A.canManage = function () {
    return !!A.me && (A.me.role === 'Admin' || A.me.role === 'Staff');
  };

  A.canCatalog = function () {
    return !!A.me && A.me.role === 'Admin';
  };

  A.defineReload = function (handler) {
    A.onReload = handler;
  };

  A.skeleton = function (blocks) {
    var card = '<div class="card card-pad"><div class="skeleton" aria-hidden="true"></div>' +
      '<div class="skeleton" aria-hidden="true"></div></div>';

    return '<div class="stack">' + new Array(blocks || 3).fill(card).join('') + '</div>';
  };

  A.empty = function (iconName, text) {
    return '<div class="empty">' + SB.icon(iconName) + '<p>' + SB.esc(text) + '</p></div>';
  };

  A.fail = function (host, error) {
    host.innerHTML = A.empty('warning', error.message || 'Te dhenat nuk u ngarkuan.');
  };

  A.handleError = function (error, host) {
    if (SB.isAuthError(error)) {
      showLogin();
      SB.toast('Sesioni u perfundua. Hyresh perseri.', 'error');
      return;
    }

    if (host) {
      A.fail(host, error);
      return;
    }

    SB.toast(error.message, 'error');
  };

  A.setActions = function (list) {
    A.actions = {};

    A.dom.viewActions.innerHTML = (list || []).filter(Boolean).map(function (item) {
      A.actions[item.id] = item.onClick;

      return '<button class="btn ' + (item.className || '') + '" type="button" data-act="' + SB.esc(item.id) + '">' +
        (item.icon ? SB.icon(item.icon, 'icon-sm') : '') + SB.esc(item.label) + '</button>';
    }).join('');
  };

  A.setSub = function (text) {
    A.dom.viewSub.textContent = text;
  };

  function showLogin() {
    A.dom.shell.classList.add('hidden');
    A.dom.loginScreen.classList.remove('hidden');
    A.me = null;
    A.onReload = null;
    SB.realtime.disconnect();
    A.setLive('off', 'Duhet te hyresh per te marre ndryshimet ne kohe reale.');

    var field = document.getElementById('Username');

    if (field) {
      field.focus();
    }
  }

  function showShell() {
    A.dom.loginScreen.classList.add('hidden');
    A.dom.shell.classList.remove('hidden');
  }

  A.signOut = function () {
    return SB.api.post('api/auth/logout').then(function () {
      showLogin();
      SB.toast('Dilja u krye. Rifresko faqe per te hyre perseri.', 'success');
    }).catch(function (error) {
      showLogin();
      SB.toast(error.message, 'error');
    });
  };

  A.setView = function (name) {
    var view = A.views[name] || A.views.dashboard;
    A.state.view = view === A.views.dashboard ? 'dashboard' : name;

    A.dom.navLinks.forEach(function (link) {
      link.classList.toggle('active', link.getAttribute('data-view') === A.state.view);
    });

    closeSide();
    A.onReload = null;
    A.dom.viewTitle.textContent = view.title;
    A.dom.viewSub.textContent = view.loadingText || 'Duke ngarkuar te dhenat';
    A.dom.viewActions.innerHTML = '';
    A.dom.viewRoot.innerHTML = A.skeleton(view.skeleton || 2);

    view.render(A.dom.viewRoot);
  };

  A.refresh = function () {
    A.setView(A.state.view);
  };

  A.refreshBadge = function () {
    return SB.api.get('api/admin/reports/summary?days=7').then(function (summary) {
      var open = summary.statusCounts.reduce(function (sum, row) {
        return row.statusKey === 'Perfunduar' ? sum : sum + row.count;
      }, 0);

      A.dom.badge.textContent = String(open);
      A.dom.badge.classList.toggle('hidden', open === 0);
      A.state.openOrders = open;
    }).catch(function () {
      return null;
    });
  };

  function closeSide() {
    A.dom.side.classList.remove('open');
    A.dom.sideScrim.classList.remove('open');
  }

  function bindShell() {
    A.dom.navLinks.forEach(function (link) {
      link.addEventListener('click', function () {
        var name = link.getAttribute('data-view');
        global.location.hash = '#' + name;
        A.setView(name);
      });
    });

    A.dom.burger.addEventListener('click', function () {
      var open = !A.dom.side.classList.contains('open');
      A.dom.side.classList.toggle('open', open);
      A.dom.sideScrim.classList.toggle('open', open);
    });

    A.dom.sideScrim.addEventListener('click', closeSide);
    A.dom.logoutBtn.addEventListener('click', A.signOut);

    A.dom.viewActions.addEventListener('click', function (event) {
      var button = event.target.closest('[data-act]');

      if (!button) {
        return;
      }

      var handler = A.actions[button.getAttribute('data-act')];

      if (handler) {
        handler(button);
      }
    });

    global.addEventListener('hashchange', function () {
      if (!A.me) {
        return;
      }

      A.setView(global.location.hash.replace('#', ''));
    });
  }

  function applySession(user) {
    A.me = user;
    A.dom.userName.textContent = user.fullName;
    A.dom.userRole.textContent = user.roleLabel || user.role;
    A.dom.userInitial.textContent = String(user.fullName || 'A').charAt(0).toUpperCase();
  }

  A.setLive = function (kind, note) {
    if (!A.dom.liveChip) {
      return;
    }

    A.dom.liveChip.classList.remove('is-live', 'is-warn', 'is-down');
    A.dom.liveChip.classList.add(kind === 'live' ? 'is-live' : kind === 'off' ? 'is-down' : 'is-warn');
    A.dom.liveChipText.textContent = kind === 'live' ? 'Direkt' : kind === 'off' ? 'Pa lidhje' : 'Rifreskim';

    if (note) {
      A.dom.liveChip.title = note;
    }
  };

  var liveReload = SB.debounce(function () {
    if (!A.me) {
      return;
    }

    if (A.onReload) {
      A.onReload();
    }

    A.refreshBadge();
  }, 500);

  function startRealtime() {
    SB.realtime.on(function (signal) {
      if (signal.kind === 'lidhje') {
        A.setLive(signal.statusKey, 'Lidhja me serverin per ndryshimet ne kohe reale.');
        return;
      }

      liveReload();
    });

    SB.realtime.connect({ url: 'api/realtime/stream', pollMs: 8000 });
  }

  function enterPanel(user) {
    applySession(user);
    showShell();
    startRealtime();
    A.setView(global.location.hash.replace('#', '') || 'dashboard');
    A.refreshBadge();
  }

  function bindLogin() {
    A.dom.loginForm.addEventListener('submit', function (event) {
      event.preventDefault();

      var button = A.dom.loginBtn;
      SB.busy(button, true, 'Duke hyre');

      SB.api.post('api/auth/login', {
        username: document.getElementById('Username').value.trim(),
        password: document.getElementById('Password').value
      }).then(function (user) {
        SB.busy(button, false);
        A.dom.loginForm.reset();

        if (user.panel && user.panel !== 'admin') {
          global.location.replace('kitchen.html');
          return;
        }

        enterPanel(user);
        SB.toast('Hyrja u krye. Paneli eshte gati per pune.', 'success');
      }).catch(function (error) {
        SB.busy(button, false);

        if (error.errors) {
          SB.applyErrors(A.dom.loginForm, error.errors);
        }

        SB.toast(error.message, 'error');
      });
    });
  }

  function boot() {
    bindShell();
    bindLogin();
    A.setLive('off', 'Duke kontrolluar sesionin.');

    SB.icons().then(function () {
      SB.api.get('api/auth/me').then(function (user) {
        if (user.panel && user.panel !== 'admin') {
          global.location.replace('kitchen.html');
          return;
        }

        enterPanel(user);

        setInterval(function () {
          if (!document.hidden) {
            A.refreshBadge();

            if (SB.realtime.state() !== 'live') {
              liveReload();
            }
          }
        }, 60000);
      }).catch(function () {
        showLogin();
      });
    });
  }

  boot();
})(window);
