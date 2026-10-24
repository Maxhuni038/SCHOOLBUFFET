(function (global) {
  var SB = global.SB = global.SB || {};
  var A = SB.A;

  var WINDOW_KEY = 'schoolbuffet.admin.window';

  var STATUS_DOT = {
    NePritje: 'dot-amber',
    DukeUPergatitur: 'dot-blue',
    Gati: 'dot-green',
    Perfunduar: 'dot-pink'
  };

  var state = {
    days: readWindow()
  };

  function readWindow() {
    try {
      var saved = Number(global.localStorage.getItem(WINDOW_KEY));
      return [7, 14, 30].indexOf(saved) >= 0 ? saved : 7;
    } catch (error) {
      return 7;
    }
  }

  function saveWindow(days) {
    state.days = days;

    try {
      global.localStorage.setItem(WINDOW_KEY, String(days));
    } catch (error) {
      return;
    }

    A.refresh();
  }

  function kpi(tone, icon, label, value, note) {
    return '<div class="card kpi ' + tone + '">' +
      '<span class="kpi-icon">' + SB.icon(icon) + '</span>' +
      '<div class="grow"><small>' + SB.esc(label) + '</small><b>' + SB.esc(value) + '</b>' +
      '<span class="note">' + SB.esc(note) + '</span></div>' +
    '</div>';
  }

  function peak(counts) {
    return Math.max.apply(null, counts.map(function (row) {
      return row.count;
    }).concat([1]));
  }

  function openCount(summary) {
    return summary.statusCounts.reduce(function (sum, row) {
      return row.statusKey === 'Perfunduar' ? sum : sum + row.count;
    }, 0);
  }

  function statusCount(summary, key) {
    var row = summary.statusCounts.filter(function (item) {
      return item.statusKey === key;
    })[0];

    return row ? row.count : 0;
  }

  function statusRows(counts) {
    var top = peak(counts);

    return '<div class="status-rows">' + counts.map(function (row) {
      var meta = SB.statusMeta(row.statusKey);

      return '<div class="status-row">' +
        '<span class="dot-swatch ' + (STATUS_DOT[row.statusKey] || 'dot-slate') + '">' + SB.icon(meta.icon, 'icon-sm') + '</span>' +
        '<div class="status-meter"><i data-meter="' + SB.esc(row.statusKey) + '"></i></div>' +
        '<b class="num">' + SB.number(row.count) + '</b>' +
      '</div>';
    }).join('') + '</div>' +
    '<p class="tiny muted section-gap">Numrat mbledhen per ' + state.days + ' dite te fundit, perfshire edhe sot.</p>';
  }

  function topList(products) {
    if (!products.length) {
      return A.empty('star', 'Ende nuk ka shitje per te radhitur produktet.');
    }

    return '<div class="top-list">' + products.map(function (item, index) {
      return '<div class="top-item"><span class="rank">' + (index + 1) + '</span>' +
        '<div class="grow"><b>' + SB.esc(item.productName) + '</b>' +
        '<p class="tiny muted">' + SB.number(item.quantity) + ' njesi te shitura</p></div>' +
        '<b class="num nowrap">' + SB.money(item.revenue) + '</b>' +
      '</div>';
    }).join('') + '</div>';
  }

  function render(host, summary) {
    var today = SB.dayKey(0);

    host.innerHTML = '<div class="kpis">' +
      kpi('kpi-green', 'wallet', 'Te ardhurat sot', SB.moneyStrict(summary.revenueToday),
        SB.moneyStrict(summary.averageOrderToday) + ' / porosi') +
      kpi('kpi-orange', 'receipt', 'Porosi sot', SB.number(summary.ordersToday),
        SB.number(summary.itemsToday) + ' artikuj') +
      kpi('kpi-blue', 'trend', 'Te ardhurat gjithsej', SB.moneyStrict(summary.revenueAll),
        SB.number(summary.ordersAll) + ' porosi') +
      kpi('kpi-amber', 'clock', 'Porosi aktive', SB.number(openCount(summary)),
        'Ne pritje, pergatitje ose gati') +
      kpi('kpi-pink', 'sandwich', 'Produktet aktive', summary.activeProducts + ' / ' + summary.totalProducts,
        summary.totalCategories + ' kategori') +
      kpi('kpi-green', 'bell', 'Gati per marrje', SB.number(statusCount(summary, 'Gati')),
        'Ne pritje nga studentet') +
    '</div>' +
    '<div class="panels">' +
      '<div class="card card-pad">' +
        '<div class="panel-title"><div><h2>Trendi i shitjeve</h2>' +
        '<p class="tiny muted">' + SB.esc(SB.plainDay(summary.date)) + ' · ' + state.days + ' dite te fundit</p></div>' +
        '<span class="pill">' + SB.moneyStrict(summary.revenueToday) + ' sot</span></div>' +
        '<div class="chart-bars" id="dashChart"></div>' +
        '<p class="tiny muted">Kliko nje shtylla per te hapur porosite e asaj date.</p>' +
      '</div>' +
      '<div class="stack">' +
        '<div class="card card-pad"><div class="panel-title"><h2>Statuset e porosive</h2>' +
        '<span class="tiny muted">' + state.days + ' dite</span></div>' + statusRows(summary.statusCounts) + '</div>' +
        '<div class="card card-pad"><div class="panel-title"><h2>Produktet me te kerkuar</h2>' +
        '<span class="tiny muted">30 dite</span></div>' + topList(summary.topProducts) + '</div>' +
      '</div>' +
    '</div>' +
    '<div class="card card-pad section-gap">' +
      '<div class="panel-title"><h2>Veprime te shpejta</h2>' +
      '<span class="tiny muted">Te dhenat rifreskohen automatikisht</span></div>' +
      '<div class="row wrap">' +
        '<button class="btn" type="button" data-jump="orders">' + SB.icon('receipt', 'icon-sm') + 'Menaxho porosite</button>' +
        '<button class="btn btn-soft" type="button" data-jump="products">' + SB.icon('plus', 'icon-sm') + 'Produkt i ri</button>' +
        '<button class="btn btn-soft" type="button" data-jump="categories">' + SB.icon('tag', 'icon-sm') + 'Kategori e re</button>' +
        '<button class="btn btn-ghost" type="button" data-jump="sales">' + SB.icon('chart', 'icon-sm') + 'Historiku i shitjeve</button>' +
        '<a class="btn btn-ghost" href="index.html" target="_blank" rel="noopener">' + SB.icon('school', 'icon-sm') + 'Menyja per studentet</a>' +
      '</div>' +
    '</div>';

    SB.charts.bars(document.getElementById('dashChart'), summary.lastDays.map(function (day) {
      return {
        label: SB.plainShort(day.date),
        value: Number(day.revenue) || 0,
        title: SB.plainDay(day.date) + ' · ' + SB.moneyStrict(day.revenue) + ' · ' + day.orders + ' porosi',
        date: day.date,
        dim: day.date !== today
      };
    }), {
      format: function (value) {
        return SB.moneyStrict(value);
      },
      onSelect: function (row) {
        A.state.dateFilter = row.date;
        global.location.hash = '#orders';
        A.setView('orders');
      }
    });

    var top = peak(summary.statusCounts);

    summary.statusCounts.forEach(function (row) {
      var meter = host.querySelector('[data-meter="' + row.statusKey + '"]');

      if (meter) {
        SB.charts.meter(meter, row.count, top, SB.statusMeta(row.statusKey).color);
      }
    });

    host.querySelectorAll('[data-jump]').forEach(function (button) {
      button.addEventListener('click', function () {
        var target = button.getAttribute('data-jump');
        global.location.hash = '#' + target;
        A.setView(target);
      });
    });
  }

  A.views.dashboard = {
    title: 'Paneli',
    skeleton: 4,
    render: function (host) {
      A.setActions([
        { id: 'w7', label: '7 dite', className: state.days === 7 ? '' : 'btn-ghost', onClick: function () { saveWindow(7); } },
        { id: 'w14', label: '14 dite', className: state.days === 14 ? '' : 'btn-ghost', onClick: function () { saveWindow(14); } },
        { id: 'w30', label: '30 dite', className: state.days === 30 ? '' : 'btn-ghost', onClick: function () { saveWindow(30); } },
        { id: 'reload', label: 'Rifresko', className: 'btn-ghost', icon: 'refresh', onClick: function () { A.refresh(); } }
      ]);

      SB.api.get('api/admin/reports/summary?days=' + state.days).then(function (summary) {
        A.state.openOrders = openCount(summary);
        A.setSub('Raporti i ' + SB.plainDay(summary.date) + ' · periudha ' + state.days + ' dite');
        render(host, summary);
        A.refreshBadge();
      }).catch(function (error) {
        A.handleError(error, host);
      });
    }
  };
})(window);
