(function (global) {
  var SB = global.SB = global.SB || {};

  var CART_KEY = 'schoolbuffet.cart.v1';
  var CODES_KEY = 'schoolbuffet.codes.v1';
  var GUEST_KEY = 'schoolbuffet.guest.v1';
  var MAX_QTY = 30;
  var MAX_LINES = 25;

  var TONES = {
    '#6c5ce7': 'dot-violet',
    '#4f7cff': 'dot-blue',
    '#16c79a': 'dot-green',
    '#8f7bff': 'dot-pink',
    '#f5a524': 'dot-amber',
    '#3ec9d7': 'dot-orange',
    '#f0426b': 'dot-red'
  };

  var dom = {
    tabs: Array.prototype.slice.call(document.querySelectorAll('.nav-tab')),
    viewMenu: document.getElementById('view-menu'),
    viewOrders: document.getElementById('view-orders'),
    search: document.getElementById('menuSearch'),
    summary: document.getElementById('menuSummary'),
    chips: document.getElementById('categoryChips'),
    menuRoot: document.getElementById('menuRoot'),
    drawer: document.getElementById('cartDrawer'),
    openCart: document.getElementById('openCart'),
    closeCart: document.getElementById('closeCart'),
    cartCount: document.getElementById('cartCount'),
    cartBody: document.getElementById('cartBody'),
    cartHint: document.getElementById('cartHint'),
    cartTotal: document.getElementById('cartTotal'),
    checkoutBtn: document.getElementById('checkoutBtn'),
    trackCode: document.getElementById('trackCode'),
    trackBtn: document.getElementById('trackBtn'),
    myOrders: document.getElementById('myOrders')
  };

  var state = {
    menu: null,
    cart: readJson(CART_KEY, []),
    codes: readJson(CODES_KEY, []),
    orders: [],
    guest: readJson(GUEST_KEY, { studentName: '', className: '' }),
    category: 'all',
    search: '',
    loadingOrders: false
  };

  function readJson(key, fallback) {
    try {
      var raw = window.localStorage.getItem(key);
      return raw ? JSON.parse(raw) : fallback;
    } catch (error) {
      return fallback;
    }
  }

  function writeJson(key, value) {
    try {
      window.localStorage.setItem(key, JSON.stringify(value));
    } catch (error) {
      return;
    }
  }

  function tone(hex) {
    return TONES[String(hex || '').toLowerCase()] || 'dot-slate';
  }

  function qtyOf(productId) {
    var line = state.cart.filter(function (item) {
      return item.id === productId;
    })[0];

    return line ? line.qty : 0;
  }

  function cartTotal() {
    return state.cart.reduce(function (sum, item) {
      return sum + Number(item.price) * item.qty;
    }, 0);
  }

  function cartUnits() {
    return state.cart.reduce(function (sum, item) {
      return sum + item.qty;
    }, 0);
  }

  function findProduct(productId) {
    if (!state.menu) {
      return null;
    }

    var found = null;

    state.menu.categories.forEach(function (category) {
      category.products.forEach(function (product) {
        if (product.id === productId) {
          found = product;
        }
      });
    });

    return found;
  }

  function showView(name) {
    dom.viewMenu.classList.toggle('hidden', name !== 'menu');
    dom.viewOrders.classList.toggle('hidden', name !== 'orders');

    dom.tabs.forEach(function (tab) {
      tab.classList.toggle('active', tab.getAttribute('data-view') === name);
    });

    if (name === 'orders') {
      loadMyOrders();
    }
  }

  function controlsHtml(product) {
    var qty = qtyOf(product.id);

    if (qty === 0) {
      return '<button class="btn btn-soft btn-sm" type="button" data-add="' + product.id + '">' +
        SB.icon('plus', 'icon-sm') + 'Shto</button>';
    }

    return '<span class="stepper">' +
      '<button type="button" data-dec="' + product.id + '" aria-label="Ul sasine">' + SB.icon('minus', 'icon-sm') + '</button>' +
      '<b>' + qty + '</b>' +
      '<button type="button" data-inc="' + product.id + '" aria-label="Rit sasine">' + SB.icon('plus', 'icon-sm') + '</button>' +
    '</span>';
  }

  function productCard(product) {
    return '<article class="card product reveal">' +
      '<div class="product-media">' +
        '<img src="' + SB.esc(product.imagePath) + '" alt="' + SB.esc(product.name) + '" loading="lazy" width="400" height="300">' +
        (product.badge ? '<span class="product-badge">' + SB.esc(product.badge) + '</span>' : '') +
        (product.isRecommended ? '<span class="product-star">' + SB.icon('star', 'icon-sm') + '</span>' : '') +
      '</div>' +
      '<div class="product-body">' +
        '<h3>' + SB.esc(product.name) + '</h3>' +
        '<p class="product-desc">' + SB.esc(product.description) + '</p>' +
        '<div class="product-foot">' +
          '<span class="price">' + SB.esc(SB.money(product.price)) + '</span>' +
          '<span data-controls="' + product.id + '">' + controlsHtml(product) + '</span>' +
        '</div>' +
      '</div>' +
    '</article>';
  }

  function matches(product, category, needle) {
    if (!needle) {
      return true;
    }

    return (product.name + ' ' + product.description + ' ' + category.name).toLowerCase().indexOf(needle) >= 0;
  }

  function visibleCategories() {
    if (!state.menu) {
      return [];
    }

    var needle = state.search.toLowerCase();

    return state.menu.categories
      .filter(function (category) {
        return state.category === 'all' || String(category.id) === state.category;
      })
      .map(function (category) {
        return {
          id: category.id,
          name: category.name,
          icon: category.icon,
          accentColor: category.accentColor,
          description: category.description,
          products: category.products.filter(function (product) {
            return matches(product, category, needle);
          })
        };
      })
      .filter(function (category) {
        return category.products.length > 0;
      });
  }

  function renderChips() {
    if (!state.menu) {
      return;
    }

    var all = state.menu.categories.reduce(function (sum, category) {
      return sum + category.products.length;
    }, 0);

    var html = '<button class="chip' + (state.category === 'all' ? ' active' : '') + '" type="button" data-cat="all">' +
      SB.icon('sparkle', 'icon-sm') + 'Te gjitha<small class="muted">' + all + '</small></button>';

    html += state.menu.categories.map(function (category) {
      return '<button class="chip' + (state.category === String(category.id) ? ' active' : '') + '" type="button" data-cat="' +
        category.id + '">' + SB.icon(category.icon, 'icon-sm') + SB.esc(category.name) +
        '<small class="muted">' + category.products.length + '</small></button>';
    }).join('');

    dom.chips.innerHTML = html;
  }

  function renderMenu() {
    renderChips();

    var categories = visibleCategories();
    var shown = categories.reduce(function (sum, category) {
      return sum + category.products.length;
    }, 0);

    dom.summary.textContent = shown === 1 ? '1 produkt gati per porositur' : shown + ' produkte gati per porositur';

    if (!categories.length) {
      dom.menuRoot.innerHTML = '<div class="empty">' + SB.icon('search') +
        '<p>Asnje produkt nuk perputhet me kerkimin. Provo fjale te tjera.</p></div>';
      return;
    }

    dom.menuRoot.innerHTML = categories.map(function (category) {
      return '<section>' +
        '<div class="cat-head">' +
          '<span class="dot ' + tone(category.accentColor) + '">' + SB.icon(category.icon) + '</span>' +
          '<div><h2>' + SB.esc(category.name) + '</h2><p>' + SB.esc(category.description) + '</p></div>' +
        '</div>' +
        '<div class="products">' + category.products.map(productCard).join('') + '</div>' +
      '</section>';
    }).join('');
  }

  function updateControls(productId) {
    var product = findProduct(productId);
    var host = dom.menuRoot.querySelector('[data-controls="' + productId + '"]');

    if (product && host) {
      host.innerHTML = controlsHtml(product);
    }
  }

  function changeQty(productId, delta) {
    var product = findProduct(productId);

    if (!product) {
      SB.toast('Produkti nuk eshte me ne meny. Rifresko faqen.', 'error');
      return;
    }

    var current = qtyOf(productId);

    if (delta > 0 && current === 0 && state.cart.length >= MAX_LINES) {
      SB.toast('Shporta mban maksimumi ' + MAX_LINES + ' produkte', 'error');
      return;
    }

    var next = current + delta;

    if (next > MAX_QTY) {
      SB.toast('Sasia maksimale per produkt eshte ' + MAX_QTY, 'error');
      return;
    }

    if (next <= 0) {
      state.cart = state.cart.filter(function (item) {
        return item.id !== productId;
      });
    } else if (current === 0) {
      state.cart.push({
        id: product.id,
        name: product.name,
        price: Number(product.price),
        image: product.imagePath,
        qty: next
      });
    } else {
      state.cart.forEach(function (item) {
        if (item.id === productId) {
          item.qty = next;
        }
      });
    }

    writeJson(CART_KEY, state.cart);
    renderCart();
    updateControls(productId);
  }

  function removeFromCart(productId) {
    state.cart = state.cart.filter(function (item) {
      return item.id !== productId;
    });

    writeJson(CART_KEY, state.cart);
    renderCart();
    updateControls(productId);
  }

  function renderCart() {
    var units = cartUnits();

    dom.cartCount.textContent = String(units);
    dom.cartCount.classList.toggle('show', units > 0);
    dom.checkoutBtn.disabled = units === 0;
    dom.cartTotal.textContent = SB.moneyStrict(cartTotal());
    dom.cartHint.textContent = units === 0
      ? 'Asnje produkt ende'
      : units + (units === 1 ? ' artikull' : ' artikuj') + ' ne shporte';

    if (!state.cart.length) {
      dom.cartBody.innerHTML = '<div class="empty">' + SB.icon('cart') +
        '<p>Shporta eshte bosh. Shto produkt nga menyja.</p></div>';
      return;
    }

    dom.cartBody.innerHTML = state.cart.map(function (item) {
      return '<div class="cart-line">' +
        '<img src="' + SB.esc(item.image) + '" alt="' + SB.esc(item.name) + '" width="54" height="44">' +
        '<div><h4>' + SB.esc(item.name) + '</h4>' +
          '<p class="tiny muted">' + SB.money(item.price) + ' x ' + item.qty + ' = ' + SB.money(item.price * item.qty) + '</p>' +
          '<span class="stepper">' +
            '<button type="button" data-dec="' + item.id + '" aria-label="Ul sasine">' + SB.icon('minus', 'icon-sm') + '</button>' +
            '<b>' + item.qty + '</b>' +
            '<button type="button" data-inc="' + item.id + '" aria-label="Rit sasine">' + SB.icon('plus', 'icon-sm') + '</button>' +
          '</span>' +
        '</div>' +
        '<button class="btn btn-ghost btn-icon" type="button" data-drop="' + item.id + '" aria-label="Heq produkt">' +
          SB.icon('trash', 'icon-sm') + '</button>' +
      '</div>';
    }).join('');
  }

  function openCart() {
    dom.drawer.classList.add('open');
    dom.closeCart.focus();
  }

  function closeCart() {
    dom.drawer.classList.remove('open');
  }

  function checkoutHtml() {
    var lines = state.cart.map(function (item) {
      return '<div><span>' + SB.esc(item.name) + ' x' + item.qty + '</span><b>' + SB.money(item.price * item.qty) + '</b></div>';
    }).join('');

    return '<div class="mini-lines">' + lines +
      '<div><span class="strong">Totali</span><b class="strong">' + SB.moneyStrict(cartTotal()) + '</b></div></div>' +
      '<div class="divider"></div>' +
      '<div class="field">' +
        '<label for="StudentName">Emri i studentit</label>' +
        '<input class="input" id="StudentName" name="StudentName" maxlength="60" autocomplete="name" placeholder="P.sh. Anxhela Krasniqi" value="' + SB.esc(state.guest.studentName) + '">' +
        '<span class="field-error"></span>' +
      '</div>' +
      '<div class="field">' +
        '<label for="ClassName">Klasa</label>' +
        '<input class="input" id="ClassName" name="ClassName" maxlength="40" autocomplete="off" placeholder="P.sh. IX B" value="' + SB.esc(state.guest.className) + '">' +
        '<span class="field-error"></span>' +
      '</div>' +
      '<div class="field">' +
        '<label for="Note">Shenim per byfen</label>' +
        '<textarea class="textarea" id="Note" name="Note" maxlength="240" placeholder="P.sh. pa qepure, me salcice"></textarea>' +
        '<span class="field-error"></span>' +
      '</div>' +
      '<p class="tiny muted">Pagesa behet ne karten e kantines. Marrja ne hyrje te byfese.</p>';
  }

  function openCheckout() {
    if (!state.cart.length) {
      SB.toast('Shto te pakten nje produkt ne shporte', 'error');
      return;
    }

    var handle = SB.modal({
      title: 'Perfundo porosine',
      subtitle: state.cart.length + (state.cart.length === 1 ? ' produkt' : ' produkte') + ' · ' + SB.moneyStrict(cartTotal()),
      html: checkoutHtml(),
      actions: [
        { id: 'cancel', label: 'Anulo', className: 'btn-ghost' },
        { id: 'place', label: 'Rregjistro porosine' }
      ],
      onAction: function (id, modal) {
        if (id === 'cancel') {
          modal.close();
          return;
        }

        submitOrder(modal);
      }
    });
  }

  function submitOrder(handle) {
    var form = handle.root;
    var button = form.querySelector('[data-action="place"]');
    var payload = {
      studentName: value('#StudentName', form),
      className: value('#ClassName', form),
      note: value('#Note', form),
      items: state.cart.map(function (item) {
        return { productId: item.id, quantity: item.qty };
      })
    };

    SB.busy(button, true, 'Duke derguar');

    SB.api.post('api/orders', payload).then(function (order) {
      state.guest = { studentName: order.studentName, className: order.className };
      writeJson(GUEST_KEY, state.guest);

      if (state.codes.indexOf(order.code) < 0) {
        state.codes.unshift(order.code);
        state.codes = state.codes.slice(0, 12);
        writeJson(CODES_KEY, state.codes);
      }

      state.orders = [order].concat(state.orders.filter(function (item) {
        return item.code !== order.code;
      }));

      state.cart = [];
      writeJson(CART_KEY, state.cart);

      handle.close();
      closeCart();
      renderCart();
      renderMenu();
      showSuccess(order);
    }).catch(function (error) {
      SB.busy(button, false);

      if (error.errors) {
        SB.applyErrors(form, error.errors);
      }

      SB.toast(error.message, 'error');
    });
  }

  function value(selector, root) {
    var node = (root || document).querySelector(selector);
    return node ? node.value.trim() : '';
  }

  function showSuccess(order) {
    var handle = SB.modal({
      title: 'Porosia u regjistrua',
      html: '<div class="success-box">' +
        '<div class="tick">' + SB.icon('check', 'icon-lg') + '</div>' +
        '<h3>Faleminderit, ' + SB.esc(order.studentName) + '</h3>' +
        '<p class="muted tiny">Byfeja e merr porositen menjehere. Tregoje kodin ne hyrje te byfese.</p>' +
        '<button class="code-copy" type="button" data-copy="' + SB.esc(order.code) + '">' +
          SB.esc(order.code) + SB.icon('copy', 'icon-sm') + '</button>' +
        '<div class="divider"></div>' +
        '<div class="mini-lines">' + order.items.map(function (item) {
          return '<div><span>' + SB.esc(item.productName) + ' x' + item.quantity + '</span><b>' + SB.money(item.lineTotal) + '</b></div>';
        }).join('') +
          '<div><span class="strong">Totali</span><b class="strong">' + SB.moneyStrict(order.totalAmount) + '</b></div>' +
        '</div>' +
      '</div>',
      actions: [
        { id: 'menu', label: 'Urdnero', className: 'btn-ghost' },
        { id: 'track', label: 'Ndiq statusin' }
      ],
      onAction: function (id, modal) {
        modal.close();

        if (id === 'track') {
          dom.trackCode.value = order.code;
          showView('orders');
        }
      }
    });

    handle.root.addEventListener('click', function (event) {
      var copyBtn = event.target.closest('[data-copy]');

      if (copyBtn) {
        copyText(copyBtn.getAttribute('data-copy'));
      }
    });
  }

  function copyText(text) {
    if (navigator.clipboard && navigator.clipboard.writeText) {
      navigator.clipboard.writeText(text).then(function () {
        SB.toast('Kodi ' + text + ' u kopjua', 'success');
      }).catch(function () {
        SB.toast('Kopjoje kodin me dor: ' + text, 'info');
      });

      return;
    }

    SB.toast('Kopjoje kodin me dor: ' + text, 'info');
  }

  function timeline(statusKey) {
    var flow = SB.statusFlow;
    var current = flow.indexOf(statusKey);

    return '<div class="timeline">' + flow.map(function (key, index) {
      var meta = SB.statusMeta(key);
      var cls = index < current ? ' done' : index === current ? ' current' : '';

      return '<div class="tstep' + cls + '"><i>' +
        SB.icon(index < current ? 'check' : meta.icon, 'icon-sm') + '</i><span>' + SB.esc(meta.label) + '</span></div>';
    }).join('') + '</div>';
  }

  function orderCard(order) {
    var meta = SB.statusMeta(order.statusKey);

    return '<article class="card order-card" data-order="' + SB.esc(order.code) + '">' +
      '<div class="order-head">' +
        '<div><span class="order-code">' + SB.esc(order.code) + '</span>' +
        '<p class="tiny muted">' + SB.esc(SB.dateTime(order.createdAtUtc)) + ' · ' + order.itemCount +
        (order.itemCount === 1 ? ' artikull' : ' artikuj') + ' · ' + SB.esc(order.studentName) +
        ' · Klasa ' + SB.esc(order.className) + '</p></div>' +
        '<span class="pill ' + meta.pill + '">' + SB.icon(meta.icon, 'icon-sm') + SB.esc(order.statusLabel) + '</span>' +
      '</div>' +
      '<div class="order-body">' +
        timeline(order.statusKey) +
        '<div class="mini-lines">' + order.items.map(function (item) {
          return '<div><span>' + SB.esc(item.productName) + ' x' + item.quantity + '</span><b>' + SB.money(item.lineTotal) + '</b></div>';
        }).join('') +
          '<div><span class="muted">Totali</span><b class="strong">' + SB.moneyStrict(order.totalAmount) + '</b></div>' +
        '</div>' +
        (order.note ? '<p class="tiny muted">' + SB.icon('info', 'icon-sm') + ' ' + SB.esc(order.note) + '</p>' : '') +
        '<div class="spread">' +
          '<span class="tiny muted">' + (order.completedAtUtc
            ? 'U mbyll ' + SB.esc(SB.time(order.completedAtUtc))
            : 'Perditesuar ' + SB.esc(SB.relative(order.updatedAtUtc))) + '</span>' +
          '<span class="row">' +
            '<button class="btn btn-ghost btn-sm" type="button" data-reload="' + SB.esc(order.code) + '">' + SB.icon('refresh', 'icon-sm') + 'Rifresko</button>' +
            '<button class="btn btn-ghost btn-sm" type="button" data-forget="' + SB.esc(order.code) + '">' + SB.icon('trash', 'icon-sm') + 'Fshij</button>' +
          '</span>' +
        '</div>' +
      '</div>' +
    '</article>';
  }

  function unknownOrderCard(code) {
    return '<article class="card order-card"><div class="order-head">' +
      '<span class="order-code">' + SB.esc(code) + '</span></div>' +
      '<div class="order-body"><p class="tiny muted">Ky kod nuk u gjet ne sistem. Provoje perseri pas pak.</p>' +
      '<div class="spread"><span></span><span class="row">' +
      '<button class="btn btn-ghost btn-sm" type="button" data-reload="' + SB.esc(code) + '">' + SB.icon('refresh', 'icon-sm') + 'Provo</button>' +
      '<button class="btn btn-ghost btn-sm" type="button" data-forget="' + SB.esc(code) + '">' + SB.icon('trash', 'icon-sm') + 'Fshij</button>' +
      '</span></div></div></article>';
  }

  function renderMyOrders() {
    if (!state.codes.length) {
      dom.myOrders.innerHTML = '<div class="empty">' + SB.icon('receipt') +
        '<p>Ende nuk ke bere porositi nga kjo pajisje. Kodet ruhen ketu automatikisht.</p></div>';
      return;
    }

    var byCode = {};

    state.orders.forEach(function (order) {
      byCode[order.code] = order;
    });

    dom.myOrders.innerHTML = state.codes.map(function (code) {
      return byCode[code] ? orderCard(byCode[code]) : unknownOrderCard(code);
    }).join('');
  }

  function loadOrder(code) {
    return SB.api.get('api/orders/track/' + encodeURIComponent(code)).catch(function (error) {
      if (error.status === 404 || error.status === 400) {
        return null;
      }

      SB.toast(error.message, 'error');
      return null;
    });
  }

  function loadMyOrders() {
    if (state.loadingOrders || !state.codes.length) {
      renderMyOrders();
      return Promise.resolve();
    }

    state.loadingOrders = true;
    dom.myOrders.innerHTML = state.codes.map(function () {
      return '<article class="card order-card"><div class="order-body"><div class="skeleton" aria-hidden="true"></div></div></article>';
    }).join('');

    return Promise.all(state.codes.map(loadOrder)).then(function (orders) {
      state.orders = orders.filter(function (order) {
        return !!order;
      });
      state.loadingOrders = false;
      renderMyOrders();
    }).catch(function (error) {
      state.loadingOrders = false;
      dom.myOrders.innerHTML = '<div class="empty">' + SB.icon('warning') +
        '<p>' + SB.esc(error.message) + '</p></div>';
    });
  }

  function trackSingle(code) {
    dom.trackBtn.disabled = true;

    loadOrder(code).then(function (order) {
      dom.trackBtn.disabled = false;

      if (!order) {
        SB.toast('Kodi ' + code + ' nuk u gjet. Kontrollo shkronjat.', 'error');
        return;
      }

      if (state.codes.indexOf(code) < 0) {
        state.codes.unshift(order.code);
        state.codes = state.codes.slice(0, 12);
        writeJson(CODES_KEY, state.codes);
      }

      state.orders = [order].concat(state.orders.filter(function (item) {
        return item.code !== order.code;
      }));

      dom.trackCode.value = order.code;
      renderMyOrders();
      SB.toast(order.code + ' · ' + order.statusLabel, 'success');
    });
  }

  function loadMenu() {
    dom.menuRoot.innerHTML = '<div class="products">' +
      new Array(8).fill('<article class="card"><div class="skeleton" aria-hidden="true"></div></article>').join('') +
      '</div>';
    dom.summary.textContent = 'Duke ngarkuar menune';

    return SB.api.get('api/menu').then(function (menu) {
      state.menu = menu;

      if (menu.currency) {
        SB.currencyLabel = menu.currency;
      }

      state.cart = state.cart.filter(function (item) {
        return item.qty > 0 && !!findProduct(item.id);
      });

      writeJson(CART_KEY, state.cart);
      renderMenu();
      renderCart();
    }).catch(function (error) {
      dom.summary.textContent = 'Menyja nuk u ngarkua';
      dom.menuRoot.innerHTML = '<div class="empty">' + SB.icon('warning') +
        '<p>' + SB.esc(error.message) + '</p>' +
        '<button class="btn btn-ghost btn-sm" type="button" id="retryMenu">' + SB.icon('refresh', 'icon-sm') + 'Provo perseri</button></div>';

      var retry = document.getElementById('retryMenu');

      if (retry) {
        retry.addEventListener('click', loadMenu);
      }
    });
  }

  function bind() {
    dom.tabs.forEach(function (tab) {
      tab.addEventListener('click', function () {
        showView(tab.getAttribute('data-view'));
      });
    });

    dom.openCart.addEventListener('click', openCart);
    dom.closeCart.addEventListener('click', closeCart);
    dom.checkoutBtn.addEventListener('click', openCheckout);

    dom.search.addEventListener('input', SB.debounce(function () {
      state.search = dom.search.value.trim();
      renderMenu();
    }, 220));

    dom.chips.addEventListener('click', function (event) {
      var chip = event.target.closest('[data-cat]');

      if (!chip) {
        return;
      }

      state.category = chip.getAttribute('data-cat');
      renderMenu();
    });

    document.addEventListener('click', function (event) {
      var inc = event.target.closest('[data-inc]');
      var dec = event.target.closest('[data-dec]');
      var add = event.target.closest('[data-add]');

      if (inc) {
        changeQty(Number(inc.getAttribute('data-inc')), 1);
        return;
      }

      if (dec) {
        changeQty(Number(dec.getAttribute('data-dec')), -1);
        return;
      }

      if (add) {
        var addId = Number(add.getAttribute('data-add'));
        changeQty(addId, 1);

        if (qtyOf(addId) === 1) {
          SB.toast('Shtuar ne shporte', 'success');
        }

        return;
      }

      var drop = event.target.closest('[data-drop]');

      if (drop) {
        removeFromCart(Number(drop.getAttribute('data-drop')));
        return;
      }

      var reload = event.target.closest('[data-reload]');

      if (reload) {
        trackSingle(reload.getAttribute('data-reload'));
        return;
      }

      var forget = event.target.closest('[data-forget]');

      if (forget) {
        var code = forget.getAttribute('data-forget');

        state.codes = state.codes.filter(function (item) {
          return item !== code;
        });
        state.orders = state.orders.filter(function (order) {
          return order.code !== code;
        });

        writeJson(CODES_KEY, state.codes);
        renderMyOrders();
        SB.toast('Porosia u heq nga lista e pajisjes', 'info');
      }
    });

    dom.trackBtn.addEventListener('click', function () {
      var code = dom.trackCode.value.trim().toUpperCase();

      if (code.length < 6) {
        SB.toast('Shkruani kodin e plote te porosise', 'error');
        return;
      }

      trackSingle(code);
    });

    dom.trackCode.addEventListener('keydown', function (event) {
      if (event.key === 'Enter') {
        event.preventDefault();
        dom.trackBtn.click();
      }
    });

    document.addEventListener('keydown', function (event) {
      if (event.key === 'Escape' && dom.drawer.classList.contains('open')) {
        closeCart();
      }
    });

    setInterval(function () {
      if (!dom.viewOrders.classList.contains('hidden') && !document.hidden && state.codes.length) {
        loadMyOrders();
      }
    }, 30000);
  }

  SB.icons().then(function () {
    renderCart();
    bind();
    loadMenu();
  });
})(window);
