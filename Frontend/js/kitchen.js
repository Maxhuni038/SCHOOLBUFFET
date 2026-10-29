(function (global) {
  var SB = global.SB = global.SB || {};
  var K = {
    me: null,
    board: null,
    seenIds: {},
    sound: true,
    audio: null,
    busy: false,
    queue: null,
    tick: null
  };

  var dom = {
    login: document.getElementById('kdsLogin'),
    loginForm: document.getElementById('kdsLoginForm'),
    loginBtn: document.getElementById('kdsLoginBtn'),
    shell: document.getElementById('kdsShell'),
    day: document.getElementById('kdsDay'),
    clock: document.getElementById('kdsClock'),
    clockDay: document.getElementById('kdsClockDay'),
    stats: document.getElementById('kdsStats'),
    link: document.getElementById('kdsLink'),
    linkText: document.getElementById('kdsLinkText'),
    soundBtn: document.getElementById('kdsSoundBtn'),
    soundText: document.getElementById('kdsSoundText'),
    refreshBtn: document.getElementById('kdsRefreshBtn'),
    logoutBtn: document.getElementById('kdsLogoutBtn'),
    seen: document.getElementById('kdsSeen'),
    footNote: document.getElementById('kdsFootNote')
  };

  var URGENT_SECONDS = 300;
  var READY_URGENT_SECONDS = 480;

  function columnBody(name) {
    return document.querySelector('[data-body="' + name + '"]');
  }

  function countNode(name) {
    return document.querySelector('[data-count="' + name + '"]');
  }

  function fmt(seconds) {
    var s = Math.max(0, Math.floor(seconds));
    var m = Math.floor(s / 60);
    var rest = s % 60;

    if (m >= 60) {
      return Math.floor(m / 60) + 'o ' + String(m % 60).padStart(2, '0') + 'm';
    }

    return m + ':' + String(rest).padStart(2, '0');
  }

  function ageSeconds(iso) {
    var base = new Date(iso).getTime();

    if (isNaN(base)) {
      return 0;
    }

    return Math.max(0, Math.round((Date.now() - base) / 1000));
  }

  function ticketCard(ticket, tone) {
    var meta = SB.statusMeta(ticket.statusKey);
    var canClose = K.me && (K.me.role === 'Admin' || K.me.role === 'Staff');
    var acts = '';

    if (tone === 'new') {
      acts = '<button class="btn btn-sm kds-btn-cook" type="button" data-act="start" data-id="' + ticket.id + '">' +
        SB.icon('pot', 'icon-sm') + 'Fillo pergatitjen</button>';
    }

    if (tone === 'preparing') {
      acts = '<button class="btn btn-sm kds-btn-ready" type="button" data-act="ready" data-id="' + ticket.id + '">' +
        SB.icon('check', 'icon-sm') + 'Sheno gati</button>' +
        '<button class="btn btn-ghost btn-sm" type="button" data-act="back" data-id="' + ticket.id + '">' +
        SB.icon('chevron', 'icon-sm') + 'Mbrapsht</button>';
    }

    if (tone === 'ready') {
      acts = '<button class="btn btn-ghost btn-sm" type="button" data-act="back" data-id="' + ticket.id + '">' +
        SB.icon('chevron', 'icon-sm') + 'Mbrapsht</button>';

      if (canClose) {
        acts += '<button class="btn btn-sm" type="button" data-act="done" data-id="' + ticket.id + '">' +
          SB.icon('check-circle', 'icon-sm') + 'Mbyll porosine</button>';
      }
    }

    var lines = ticket.items.map(function (item) {
      return '<div class="kds-line"><span class="kds-line-qty">' + item.quantity + 'x</span>' +
        '<span>' + SB.esc(item.productName) + '</span></div>';
    }).join('');

    return '<article class="kds-card" data-tone="' + tone + '" data-id="' + ticket.id + '"' +
      ' data-status="' + SB.esc(ticket.statusKey) + '"' +
      ' data-created="' + ticket.createdAtUtc + '"' +
      ' data-preparing="' + (ticket.preparingAtUtc || '') + '"' +
      ' data-ready="' + (ticket.readyAtUtc || '') + '"' +
      ' data-urgent="' + (tone === 'ready' ? READY_URGENT_SECONDS : URGENT_SECONDS) + '">' +
        '<div class="kds-card-top">' +
          '<span class="kds-code">' + SB.esc(ticket.code) + '</span>' +
          '<span class="kds-timer"><b>--:--</b><small>' + SB.esc(meta.label) + '</small></span>' +
        '</div>' +
        '<div class="kds-who">' + SB.esc(ticket.studentName) + '<span>' + SB.esc(ticket.className) + '</span></div>' +
        '<div class="kds-lines">' + lines + '</div>' +
        (ticket.note
          ? '<div class="kds-note">' + SB.icon('warning', 'icon-sm') + '<span>' + SB.esc(ticket.note) + '</span></div>'
          : '') +
        '<div class="kds-acts">' + acts + '</div>' +
      '</article>';
  }

  function renderColumn(name, tickets) {
    var body = columnBody(name);
    var holder = countNode(name);

    if (holder) {
      holder.textContent = String(tickets.length);
    }

    if (!body) {
      return;
    }

    if (!tickets.length) {
      body.innerHTML = '<div class="kds-none">' +
        (name === 'new' ? 'Asnje poros ne pritje.' : name === 'preparing' ? 'Asnje poros nuk po pergatitet.' : 'Asnje poros nuk eshte gati.') +
        '</div>';
      return;
    }

    body.innerHTML = tickets.map(function (ticket) {
      return ticketCard(ticket, name);
    }).join('');
  }

  function renderStats(stats) {
    if (!stats) {
      return;
    }

    dom.stats.innerHTML =
      '<span class="kds-chip kds-chip-amber">Ne pritje <b>' + stats.newCount + '</b></span>' +
      '<span class="kds-chip kds-chip-blue">Duke u pergatitur <b>' + stats.preparingCount + '</b></span>' +
      '<span class="kds-chip kds-chip-green">Gati <b>' + stats.readyCount + '</b></span>' +
      '<span class="kds-chip">Porosi sot <b>' + stats.ordersToday + '</b></span>' +
      '<span class="kds-chip">Artikuj sot <b>' + stats.itemsToday + '</b></span>' +
      '<span class="kds-chip">Koha mesatare <b>' + (stats.averagePrepMinutes || 0) + ' min</b></span>' +
      '<span class="kds-chip">Ne radhe me e vjeter <b>' + fmt(stats.oldestWaitingSeconds) + '</b></span>' +
      '<span class="kds-chip">Ekrane te lidhur <b>' + stats.connectedScreens + '</b></span>';
  }

  function paintClock() {
    var now = new Date();

    if (dom.clock) {
      dom.clock.textContent = [
        String(now.getHours()).padStart(2, '0'),
        String(now.getMinutes()).padStart(2, '0'),
        String(now.getSeconds()).padStart(2, '0')
      ].join(':');
    }

    if (dom.clockDay) {
      dom.clockDay.textContent = SB.weekday(SB.todayKey()) + ', ' + SB.plainDay(SB.todayKey());
    }
  }

  function paintTimers() {
    paintClock();

    Array.prototype.forEach.call(document.querySelectorAll('.kds-card'), function (card) {
      var tone = card.getAttribute('data-tone');
      var created = card.getAttribute('data-created');
      var preparing = card.getAttribute('data-preparing');
      var ready = card.getAttribute('data-ready');
      var urgent = Number(card.getAttribute('data-urgent')) || URGENT_SECONDS;
      var label = card.querySelector('.kds-timer b');
      var small = card.querySelector('.kds-timer small');

      var from = tone === 'ready' && ready ? ready : (tone === 'preparing' && preparing ? preparing : created);
      var seconds = ageSeconds(from);

      if (label) {
        label.textContent = fmt(seconds);
      }

      if (small) {
        small.textContent = tone === 'ready'
          ? 'Pritet dorzimi'
          : tone === 'preparing' && preparing
            ? 'Pergatitet qe nga ' + SB.time(preparing)
            : SB.statusMeta(card.getAttribute('data-status') || 'NePritje').label;
      }

      card.classList.toggle('is-urgent', tone === 'ready' ? seconds > READY_URGENT_SECONDS : seconds > urgent);
    });

    if (dom.seen) {
      dom.seen.textContent = String(Math.max(0, Math.round((Date.now() - (K.lastSignal || Date.now())) / 1000)));
    }
  }

  function beep(kind) {
    if (!K.sound) {
      return;
    }

    try {
      var Ctx = global.AudioContext || global.webkitAudioContext;

      if (!Ctx) {
        return;
      }

      if (!K.audio) {
        K.audio = new Ctx();
      }

      if (K.audio.state === 'suspended') {
        K.audio.resume();
      }

      var notes = kind === 'ready' ? [880, 1180] : [620, 880, 1180];

      notes.forEach(function (freq, index) {
        var osc = K.audio.createOscillator();
        var gain = K.audio.createGain();
        var startAt = K.audio.currentTime + index * 0.13;

        osc.type = 'triangle';
        osc.frequency.value = freq;
        gain.gain.setValueAtTime(0.0001, startAt);
        gain.gain.exponentialRampToValueAtTime(0.16, startAt + 0.03);
        gain.gain.exponentialRampToValueAtTime(0.0001, startAt + 0.22);
        osc.connect(gain);
        gain.connect(K.audio.destination);
        osc.start(startAt);
        osc.stop(startAt + 0.26);
      });
    } catch (error) {
      return;
    }
  }

  function detectFresh(board) {
    var incoming = [];
    var seenBefore = Object.keys(K.seenIds).length > 0;

    ['new', 'preparing', 'ready'].forEach(function (tone) {
      (board[tone] || []).forEach(function (ticket) {
        incoming.push(ticket.id);

        if (seenBefore && !K.seenIds[ticket.id] && tone === 'new') {
          beep('order');
        }
      });
    });

    if (seenBefore && board.ready && board.ready.length) {
      var newReady = board.ready.filter(function (ticket) {
        return K.seenIds[ticket.id] && K.seenIds[ticket.id] !== 'ready';
      });

      if (newReady.length) {
        beep('ready');
      }
    }

    K.seenIds = {};
    incoming.forEach(function (id) {
      K.seenIds[id] = 'seen';
    });

    (board.ready || []).forEach(function (ticket) {
      K.seenIds[ticket.id] = 'ready';
    });

    Array.prototype.forEach.call(document.querySelectorAll('.kds-card'), function (card) {
      var id = Number(card.getAttribute('data-id'));

      if (seenBefore && incoming.indexOf(id) === -1) {
        return;
      }

      if (seenBefore && !card.classList.contains('is-fresh')) {
        card.classList.add('is-fresh');
        global.setTimeout(function () {
          card.classList.remove('is-fresh');
        }, 620);
      }
    });
  }

  function paint(board) {
    K.board = board;
    dom.day.textContent = SB.plainDay(board.day) + ' · ' + (K.me ? K.me.roleLabel : 'Kuzhina');
    renderStats(board.stats);
    renderColumn('new', board.new);
    renderColumn('preparing', board.preparing);
    renderColumn('ready', board.ready);
    paintTimers();
  }

  function load(options) {
    var settings = options || {};

    if (K.busy && !settings.force) {
      K.queue = settings.silent;
      return Promise.resolve(null);
    }

    K.busy = true;

    return SB.api.get('api/kitchen/board').then(function (board) {
      K.busy = false;
      K.lastSignal = Date.now();
      paint(board);
      detectFresh(board);
      setLink('ok', settings.silent ? 'Pamja eshte e pertepshte' : 'Te dhenat u rifreskuan');

      if (K.queue !== null) {
        var silent = K.queue;
        K.queue = null;
        load({ silent: silent });
      }

      return board;
    }).catch(function (error) {
      K.busy = false;

      if (SB.isAuthError(error)) {
        showLogin();
        SB.toast(error.message, 'error');
        return null;
      }

      setLink('down', error.message || 'Serveri nuk arrihet');

      if (!settings.silent) {
        SB.toast(error.message, 'error');
      }

      return null;
    });
  }

  function setLink(kind, note) {
    dom.link.classList.remove('is-live', 'is-warn', 'is-down');

    if (kind === 'ok') {
      dom.link.classList.add('is-live');
      dom.linkText.textContent = 'Direkt';
    } else if (kind === 'warn') {
      dom.link.classList.add('is-warn');
      dom.linkText.textContent = 'Rifreskim';
    } else {
      dom.link.classList.add('is-down');
      dom.linkText.textContent = 'Pa lidhje';
    }

    if (note && dom.footNote) {
      dom.footNote.textContent = note;
    }
  }

  function runAction(id, act, button) {
    SB.busy(button, true, 'Duke derguar');

    SB.api.post('api/kitchen/' + id + '/' + act, {}).then(function () {
      SB.busy(button, false);
      load({ silent: true, force: true });

      if (act === 'ready') {
        SB.toast('Porosia u shenua Gati. Studenti dhe paneli e marre njoftimin menjehere.', 'success');
      } else if (act === 'done') {
        SB.toast('Porosia u mbyll dhe pagesa u shenua e paguar.', 'success');
      } else if (act === 'start') {
        SB.toast('Porosia hyri ne pergatitje.', 'success');
      } else {
        SB.toast('Statusi u kthye mbrapsht.', 'info');
      }
    }).catch(function (error) {
      SB.busy(button, false);

      if (SB.isAuthError(error)) {
        showLogin();
        SB.toast(error.message, 'error');
        return;
      }

      SB.toast(error.message, 'error');
      load({ silent: true, force: true });
    });
  }

  function showLogin() {
    K.me = null;
    SB.realtime.disconnect();
    dom.shell.classList.add('hidden');
    dom.login.classList.remove('hidden');

    var field = document.getElementById('Username');

    if (field) {
      field.focus();
    }
  }

  function startSession(user) {
    K.me = user;
    dom.login.classList.add('hidden');
    dom.shell.classList.remove('hidden');

    if (K.tick) {
      clearInterval(K.tick);
    }

    K.tick = setInterval(paintTimers, 1000);

    SB.realtime.on(function (signal) {
      K.lastSignal = Date.now();

      if (signal.kind === 'lidhje') {
        if (signal.statusKey === 'live') {
          setLink('ok', 'Lidhja direkt me serverin eshte aktive.');
        } else if (signal.statusKey === 'polling') {
          setLink('warn', 'Lidhja direkt ra. Rifreskojme automatikisht cdo 6 sekonda.');
        } else if (signal.statusKey === 'reconnect') {
          setLink('warn', 'Riperpjekje me serverin...');
        }

        return;
      }

      load({ silent: true, force: true });
    });

    SB.realtime.connect({ url: 'api/realtime/stream', pollMs: 6000 });
    load({ force: true });
  }

  function bind() {
    dom.loginForm.addEventListener('submit', function (event) {
      event.preventDefault();
      SB.busy(dom.loginBtn, true, 'Duke hyre');

      SB.api.post('api/auth/login', {
        username: document.getElementById('Username').value.trim(),
        password: document.getElementById('Password').value
      }).then(function (user) {
        SB.busy(dom.loginBtn, false);
        dom.loginForm.reset();
        K.sound = true;
        startSession(user);
        SB.toast('Ekrani i kuzhines u hap per ' + user.fullName + '.', 'success');
      }).catch(function (error) {
        SB.busy(dom.loginBtn, false);

        if (error.errors) {
          SB.applyErrors(dom.loginForm, error.errors);
        }

        SB.toast(error.message, 'error');
      });
    });

    dom.soundBtn.addEventListener('click', function () {
      K.sound = !K.sound;
      dom.soundBtn.setAttribute('aria-pressed', String(K.sound));
      dom.soundText.textContent = K.sound ? 'Tingulli ON' : 'Tingulli OFF';

      if (K.sound) {
        beep('ready');
      }
    });

    dom.refreshBtn.addEventListener('click', function () {
      SB.busy(dom.refreshBtn, true, 'Duke rifreskuar');
      load({ force: true }).then(function () {
        SB.busy(dom.refreshBtn, false);
      });
    });

    dom.logoutBtn.addEventListener('click', function () {
      SB.api.post('api/auth/logout').then(function () {
        showLogin();
        SB.toast('Dilja u krye.', 'success');
      }).catch(function (error) {
        SB.toast(error.message, 'error');
      });
    });

    dom.shell.addEventListener('click', function (event) {
      var button = event.target.closest('[data-act]');

      if (!button) {
        return;
      }

      runAction(Number(button.getAttribute('data-id')), button.getAttribute('data-act'), button);
    });

    global.setInterval(function () {
      if (document.hidden || !K.me) {
        return;
      }

      if (SB.realtime.state() !== 'live') {
        load({ silent: true });
      }
    }, 15000);
  }

  function boot() {
    K.lastSignal = Date.now();
    bind();

    SB.icons().then(function () {
      SB.api.get('api/auth/me').then(function (user) {
        startSession(user);
      }).catch(function () {
        showLogin();
      });
    });
  }

  boot();
})(window);
