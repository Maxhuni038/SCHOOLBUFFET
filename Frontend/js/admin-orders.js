(function (global) {
  var SB = global.SB = global.SB || {};
  var A = SB.A;

  var PAGE_SIZE = 12;

  var state = {
    status: '',
    search: '',
    date: '',
    page: 1,
    list: null,
    busyId: 0
  };

  function findOrder(id) {
    if (!state.list) {
      return null;
    }

    return state.list.orders.filter(function (order) {
      return order.id === id;
    })[0] || null;
  }

  function query() {
    var bits = ['pageSize=' + PAGE_SIZE, 'page=' + state.page];

    if (state.status) {
      bits.push('status=' + encodeURIComponent(state.status));
    }

    if (state.search) {
      bits.push('search=' + encodeURIComponent(state.search));
    }

    if (state.date) {
      bits.push('date=' + encodeURIComponent(state.date));
    }

    return bits.join('&');
  }

  function statusActions(order) {
    return '<div class="status-actions">' + SB.statusFlow.map(function (key) {
      var meta = SB.statusMeta(key);
      var on = key === order.statusKey;

      return '<button class="status-btn' + (on ? ' on' : '') + '" type="button" data-status="' + order.id +
        ':' + key + '"' + (on ? ' disabled' : '') + '>' + SB.icon(meta.icon, 'icon-sm') + SB.esc(meta.label) + '</button>';
    }).join('') + '</div>';
  }

  function row(order) {
    var meta = SB.statusMeta(order.statusKey);

    return '<div class="order-row" data-row="' + order.id + '">' +
      '<div class="order-row-head">' +
        '<span class="order-code">' + SB.esc(order.code) + '</span>' +
        '<div class="grow"><b>' + SB.esc(order.studentName) + '</b>' +
        '<p class="meta">' + SB.esc(order.className) + ' · ' + SB.esc(SB.dateTime(order.createdAtUtc)) +
        ' · ' + SB.esc(SB.relative(order.updatedAtUtc)) + (order.handledBy ? ' · ' + SB.esc(order.handledBy) : '') + '</p></div>' +
        '<span class="pill ' + meta.pill + '">' + SB.icon(meta.icon, 'icon-sm') + SB.esc(order.statusLabel) + '</span>' +
        '<b class="num nowrap">' + SB.moneyStrict(order.totalAmount) + '</b>' +
      '</div>' +
      '<div class="order-lines">' + order.items.map(function (item) {
        return '<span class="line-tag">' + SB.esc(item.productName) + ' x' + item.quantity + '</span>';
      }).join('') + (order.note ? '<span class="line-tag">' + SB.esc(order.note) + '</span>' : '') + '</div>' +
      '<div class="spread">' + statusActions(order) +
        '<div class="row-actions">' +
          '<button class="btn btn-ghost btn-sm" type="button" data-open="' + order.id + '">' + SB.icon('list', 'icon-sm') + 'Hollesite</button>' +
          '<button class="btn btn-ghost btn-sm" type="button" data-del="' + order.id + '">' + SB.icon('trash', 'icon-sm') + 'Fshij</button>' +
        '</div>' +
      '</div>' +
    '</div>';
  }

  function toolbar() {
    return '<div class="filter-bar">' +
      '<label class="search"><span class="sr-only">Kerko porosi</span>' + SB.icon('search') +
      '<input class="input" id="orderSearch" placeholder="Kodi, emri i studentit apo klasa" value="' + SB.esc(state.search) + '"></label>' +
      '<select class="select" id="orderStatus" aria-label="Filtro sipas statusit">' +
        '<option value="">Gjitha statuset</option>' +
        SB.statusFlow.map(function (key) {
          return '<option value="' + key + '"' + (state.status === key ? ' selected' : '') + '>' +
            SB.esc(SB.statusMeta(key).label) + '</option>';
        }).join('') +
      '</select>' +
      '<input class="input" id="orderDate" type="date" value="' + SB.esc(state.date) + '" aria-label="Filtro sipas dates">' +
      '<button class="btn btn-ghost btn-sm" type="button" id="clearFilters">' + SB.icon('close', 'icon-sm') + 'Pastro filtrat</button>' +
    '</div>';
  }

  function pager() {
    var list = state.list;
    var pages = Math.max(1, Math.ceil(list.total / PAGE_SIZE));

    return '<div class="pager"><span>' + SB.number(list.total) + ' porosi · faqja ' + list.page + ' / ' + pages +
      (list.date ? ' · ' + SB.esc(SB.plainDay(list.date)) : '') +
      (list.statusFilter ? ' · ' + SB.esc(SB.statusMeta(list.statusFilter).label) : '') + '</span>' +
      '<span class="row">' +
        '<button class="btn btn-ghost btn-sm" type="button" data-page="' + (list.page - 1) + '"' +
        (list.page <= 1 ? ' disabled' : '') + '>Para</button>' +
        '<button class="btn btn-ghost btn-sm" type="button" data-page="' + (list.page + 1) + '"' +
        (list.page >= pages ? ' disabled' : '') + '>Pas' + SB.icon('chevron', 'icon-sm') + '</button>' +
      '</span></div>';
  }

  function bindToolbar(root, host) {
    var search = root.querySelector('#orderSearch');
    var status = root.querySelector('#orderStatus');
    var date = root.querySelector('#orderDate');
    var clear = root.querySelector('#clearFilters');

    search.addEventListener('input', SB.debounce(function () {
      state.search = search.value.trim();
      state.page = 1;
      load(host);
    }, 320));

    status.addEventListener('change', function () {
      state.status = status.value;
      state.page = 1;
      load(host);
    });

    date.addEventListener('change', function () {
      state.date = date.value;
      state.page = 1;
      load(host);
    });

    clear.addEventListener('click', function () {
      state.status = '';
      state.search = '';
      state.date = '';
      state.page = 1;
      load(host);
    });
  }

  function paint(host) {
    var list = state.list;

    host.innerHTML = '<div class="stack" data-orders-root>' + toolbar() +
      (list.orders.length
        ? '<div class="orders">' + list.orders.map(row).join('') + '</div>' + pager()
        : A.empty('receipt', 'Asnje porosie nuk perputhet me filtrat. Ndrysho kriteret ose rifresko.')) +
    '</div>';

    var root = host.querySelector('[data-orders-root]');

    root.addEventListener('click', function (event) {
      var statusBtn = event.target.closest('[data-status]');

      if (statusBtn) {
        var pair = statusBtn.getAttribute('data-status').split(':');
        changeStatus(host, Number(pair[0]), pair[1]);
        return;
      }

      var pageBtn = event.target.closest('[data-page]');

      if (pageBtn) {
        state.page = Number(pageBtn.getAttribute('data-page'));
        load(host);
        return;
      }

      var delBtn = event.target.closest('[data-del]');

      if (delBtn) {
        var order = findOrder(Number(delBtn.getAttribute('data-del')));

        if (order) {
          removeOrder(host, order);
        }

        return;
      }

      var openBtn = event.target.closest('[data-open]');

      if (openBtn) {
        var detail = findOrder(Number(openBtn.getAttribute('data-open')));

        if (detail) {
          openDetail(detail);
        }
      }
    });

    bindToolbar(root, host);
  }

  function load(host) {
    return SB.api.get('api/admin/orders?' + query()).then(function (list) {
      state.list = list;
      A.setSub(SB.number(list.total) + ' porosi' +
        (list.statusFilter ? ' me statusin ' + SB.statusMeta(list.statusFilter).label : '') +
        (list.date ? ' · data ' + SB.plainDay(list.date) : ''));
      paint(host);
      A.refreshBadge();
    }).catch(function (error) {
      A.handleError(error, host);
    });
  }

  function changeStatus(host, id, key) {
    if (state.busyId) {
      return;
    }

    state.busyId = id;
    var buttons = host.querySelectorAll('[data-status^="' + id + ':"]');

    Array.prototype.forEach.call(buttons, function (button) {
      button.disabled = true;
    });

    SB.api.patch('api/admin/orders/' + id + '/status', { status: key }).then(function () {
      state.busyId = 0;
      SB.toast('Statusi u perditesua: ' + SB.statusMeta(key).label, 'success');
      return load(host);
    }).catch(function (error) {
      state.busyId = 0;

      if (SB.isAuthError(error)) {
        A.handleError(error, host);
        return;
      }

      Array.prototype.forEach.call(buttons, function (button) {
        button.disabled = false;
      });

      SB.toast(error.message, 'error');
    });
  }

  function removeOrder(host, order) {
    SB.confirm({
      title: 'Fshij porosine ' + order.code,
      text: 'Porosia dhe artikujt e saj hiqen nga regjistrat. Ky veprim nuk kthehet mbrapsht.',
      okLabel: 'Po, fshije',
      danger: true
    }).then(function (yes) {
      if (!yes) {
        return;
      }

      SB.api.del('api/admin/orders/' + order.id).then(function (result) {
        SB.toast(result.message || 'Porosia u fshi', 'success');
        return load(host);
      }).catch(function (error) {
        SB.toast(error.message, 'error');
      });
    });
  }

  function openDetail(order) {
    var meta = SB.statusMeta(order.statusKey);
    var retired = order.items.filter(function (item) {
      return !item.productId;
    }).length;

    SB.modal({
      title: 'Porosia ' + order.code,
      subtitle: order.studentName + ' · ' + order.className,
      html: '<div class="spread mb"><span class="pill ' + meta.pill + '">' + SB.icon(meta.icon, 'icon-sm') +
        SB.esc(order.statusLabel) + '</span><b>' + SB.moneyStrict(order.totalAmount) + '</b></div>' +
        '<div class="table-wrap"><table class="data"><thead><tr><th>Produkti</th><th class="num">Cmimi</th>' +
        '<th class="num">Sasia</th><th class="num">Totali</th></tr></thead><tbody>' +
        order.items.map(function (item) {
          return '<tr><td>' + SB.esc(item.productName) + '</td><td class="num">' + SB.money(item.unitPrice) +
            '</td><td class="num">' + item.quantity + '</td><td class="num">' + SB.money(item.lineTotal) + '</td></tr>';
        }).join('') + '</tbody></table></div>' +
        '<div class="stat-list section-gap">' +
          '<div class="stat-line"><span>Regjistruar</span><b>' + SB.esc(SB.dateTime(order.createdAtUtc)) + '</b></div>' +
          '<div class="stat-line"><span>Perditesuar</span><b>' + SB.esc(SB.dateTime(order.updatedAtUtc)) + '</b></div>' +
          (order.completedAtUtc
            ? '<div class="stat-line"><span>Mbyllur</span><b>' + SB.esc(SB.dateTime(order.completedAtUtc)) + '</b></div>'
            : '') +
          (order.handledBy
            ? '<div class="stat-line"><span>Menaxhuar nga</span><b>' + SB.esc(order.handledBy) + '</b></div>'
            : '') +
          (order.note
            ? '<div class="stat-line"><span>Shenim</span><b>' + SB.esc(order.note) + '</b></div>'
            : '') +
        '</div>' +
        (retired
          ? '<p class="note-box section-gap">' + SB.icon('info', 'icon-sm') +
            'Produktet e fshira nga katalogu ruhen me emer dhe cmim te vjeter brenda porosise.</p>'
          : ''),
      actions: [{ id: 'close', label: 'Mbyll', className: 'btn-ghost' }],
      onAction: function (id, modal) {
        modal.close();
      }
    });
  }

  A.views.orders = {
    title: 'Porosite',
    skeleton: 3,
    render: function (host) {
      if (A.state.dateFilter) {
        state.date = A.state.dateFilter;
        state.page = 1;
        A.state.dateFilter = '';
      }

      A.setActions([
        {
          id: 'today',
          label: 'Porosite e sotme',
          className: 'btn-ghost',
          icon: 'calendar',
          onClick: function () {
            state.date = SB.dayKey(0);
            state.status = '';
            state.page = 1;
            load(host);
          }
        },
        {
          id: 'open',
          label: 'Ne pritje',
          className: 'btn-ghost',
          icon: 'clock',
          onClick: function () {
            state.status = 'NePritje';
            state.date = '';
            state.page = 1;
            load(host);
          }
        },
        {
          id: 'reload',
          label: 'Rifresko',
          className: 'btn-ghost',
          icon: 'refresh',
          onClick: function () {
            load(host);
          }
        }
      ]);

      load(host);
    }
  };
})(window);
