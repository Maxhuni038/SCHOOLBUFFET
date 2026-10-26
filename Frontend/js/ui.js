(function (global) {
  var SB = global.SB = global.SB || {};

  SB.icon = function (name, className) {
    return '<svg class="icon ' + (className || '') + '" aria-hidden="true"><use href="#i-' + name + '"></use></svg>';
  };

  SB.debounce = function (fn, wait) {
    var timer = null;
    return function () {
      var args = arguments;
      var self = this;
      clearTimeout(timer);
      timer = setTimeout(function () {
        fn.apply(self, args);
      }, wait || 260);
    };
  };

  function ensureToasts() {
    var host = document.getElementById('toasts');
    if (!host) {
      host = document.createElement('div');
      host.id = 'toasts';
      host.className = 'toasts';
      document.body.appendChild(host);
    }

    return host;
  }

  SB.toast = function (message, type) {
    var host = ensureToasts();
    var node = document.createElement('div');
    var iconName = type === 'error' ? 'warning' : type === 'info' ? 'info' : 'check-circle';

    node.className = 'toast' + (type ? ' toast-' + type : '');
    node.setAttribute('role', type === 'error' ? 'alert' : 'status');
    node.innerHTML = SB.icon(iconName, 'icon-sm') + '<span class="grow">' + SB.esc(message) + '</span>';
    host.appendChild(node);

    setTimeout(function () {
      node.style.transition = 'opacity .25s, transform .25s';
      node.style.opacity = '0';
      node.style.transform = 'translateY(8px)';
      setTimeout(function () {
        node.remove();
      }, 260);
    }, type === 'error' ? 5200 : 3000);
  };

  var openModals = [];

  SB.modal = function (options) {
    var settings = options || {};
    var backdrop = document.createElement('div');
    backdrop.className = 'modal-backdrop';

    var footer = (settings.actions || []).map(function (action) {
      return '<button type="button" class="btn ' + (action.className || 'btn-ghost') + '" data-action="' +
        SB.esc(action.id) + '">' + SB.esc(action.label) + '</button>';
    }).join('');

    backdrop.innerHTML =
      '<div class="modal" role="dialog" aria-modal="true" aria-label="' + SB.esc(settings.title || 'Dritare') + '">' +
        '<div class="modal-head">' +
          '<div><h2>' + SB.esc(settings.title || '') + '</h2>' +
          (settings.subtitle ? '<p class="muted tiny">' + SB.esc(settings.subtitle) + '</p>' : '') + '</div>' +
          '<button type="button" class="btn btn-ghost btn-icon" data-close aria-label="Mbyll">' + SB.icon('close') + '</button>' +
        '</div>' +
        '<div class="modal-body">' + (settings.html || '') + '</div>' +
        (footer || settings.footHtml ? '<div class="modal-foot">' + footer + (settings.footHtml || '') + '</div>' : '') +
      '</div>';

    var handle = {
      root: backdrop,
      body: backdrop.querySelector('.modal-body'),
      close: function () {
        if (!backdrop.isConnected) {
          return;
        }

        backdrop.remove();
        openModals = openModals.filter(function (item) {
          return item !== handle;
        });

        if (typeof settings.onClose === 'function') {
          settings.onClose();
        }
      }
    };

    backdrop.addEventListener('click', function (event) {
      if (event.target === backdrop) {
        handle.close();
        return;
      }

      var closeBtn = event.target.closest('[data-close]');
      if (closeBtn) {
        handle.close();
        return;
      }

      var actionBtn = event.target.closest('[data-action]');
      if (actionBtn && typeof settings.onAction === 'function') {
        settings.onAction(actionBtn.getAttribute('data-action'), handle);
      }
    });

    document.body.appendChild(backdrop);
    openModals.push(handle);

    var focusable = backdrop.querySelector('input, select, textarea, button.btn:not([data-close])');
    if (focusable) {
      focusable.focus();
    }

    return handle;
  };

  SB.confirm = function (options) {
    var settings = options || {};

    return new Promise(function (resolve) {
      var confirmed = false;

      var handle = SB.modal({
        title: settings.title || 'Konfirmo veprimin',
        html: '<p>' + SB.esc(settings.text || 'Je i sigurt per kete veprim?') + '</p>',
        actions: [
          { id: 'no', label: settings.cancelLabel || 'Anulo', className: 'btn-ghost' },
          { id: 'yes', label: settings.okLabel || 'Po, vijolo', className: settings.danger ? 'btn-danger' : '' }
        ],
        onClose: function () {
          resolve(confirmed);
        },
        onAction: function (id) {
          confirmed = id === 'yes';
          handle.close();
        }
      });
    });
  };

  SB.busy = function (button, on, busyLabel) {
    if (!button) {
      return;
    }

    if (on) {
      if (!button.dataset.idleHtml) {
        button.dataset.idleHtml = button.innerHTML;
      }

      var tone = button.classList.contains('btn-ghost') || button.classList.contains('btn-soft')
        ? 'spinner spinner-dark'
        : 'spinner';

      button.disabled = true;
      button.innerHTML = '<span class="' + tone + '" aria-hidden="true"></span>' +
        (busyLabel ? '<span>' + SB.esc(busyLabel) + '</span>' : '');
      return;
    }

    button.disabled = false;

    if (button.dataset.idleHtml) {
      button.innerHTML = button.dataset.idleHtml;
    }
  };

  SB.applyErrors = function (form, errors) {
    Array.prototype.forEach.call(form.querySelectorAll('.field'), function (field) {
      field.classList.remove('invalid');
      var note = field.querySelector('.field-error');

      if (note) {
        note.textContent = '';
      }
    });

    if (!errors) {
      return;
    }

    var indexes = {};
    Array.prototype.forEach.call(form.querySelectorAll('[name]'), function (input) {
      indexes[input.getAttribute('name').toLowerCase()] = input;
    });

    Object.keys(errors).forEach(function (key) {
      var clean = key.replace(/\[\d+\]/g, '').split('.').pop().toLowerCase();
      var input = indexes[clean];

      if (!input) {
        return;
      }

      var field = input.closest('.field');

      if (!field) {
        return;
      }

      field.classList.add('invalid');
      var note = field.querySelector('.field-error');

      if (note) {
        note.textContent = (errors[key] || [])[0] || 'Vlera nuk eshte e sakte';
      }
    });
  };

  SB.escapeMode = false;

  var iconPromise = null;

  SB.icons = function () {
    if (iconPromise) {
      return iconPromise;
    }

    iconPromise = fetch('assets/sprite.svg', { credentials: 'same-origin' }).then(function (response) {
      return response.ok ? response.text() : '';
    }).then(function (text) {
      if (!text) {
        return;
      }

      var parsed = new DOMParser().parseFromString(text, 'image/svg+xml');
      var root = parsed.documentElement;

      if (!root || root.nodeName !== 'svg' || root.getElementsByTagName('parsererror').length) {
        return;
      }

      var node = document.importNode(root, true);
      node.setAttribute('class', 'sprite');
      node.setAttribute('aria-hidden', 'true');
      node.setAttribute('focusable', 'false');
      document.body.insertBefore(node, document.body.firstChild);
    }).catch(function () {
      return;
    });

    return iconPromise;
  };

  document.addEventListener('keydown', function (event) {
    if (event.key === 'Escape' && openModals.length) {
      openModals[openModals.length - 1].close();
    }
  });
})(window);
