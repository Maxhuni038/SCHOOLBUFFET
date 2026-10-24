(function (global) {
  var SB = global.SB = global.SB || {};

  SB.charts = {};

  SB.charts.bars = function (host, rows, options) {
    var settings = options || {};
    var max = rows.reduce(function (peak, row) {
      return Math.max(peak, Number(row.value) || 0);
    }, 0);

    if (max <= 0) {
      max = 1;
    }

    host.innerHTML = '';

    rows.forEach(function (row, index) {
      var column = document.createElement('div');
      column.className = 'bar-col';

      var track = document.createElement('div');
      track.className = 'bar-track';

      var bar = document.createElement('div');
      bar.className = 'bar' + (row.dim ? ' dim' : '');
      bar.style.height = '0%';
      bar.title = row.title || (row.label + ': ' + (settings.format ? settings.format(row.value) : row.value));

      if (settings.onSelect) {
        bar.addEventListener('click', function () {
          settings.onSelect(row);
        });

        bar.addEventListener('keydown', function (event) {
          if (event.key === 'Enter' || event.key === ' ') {
            event.preventDefault();
            settings.onSelect(row);
          }
        });

        bar.tabIndex = 0;
        bar.setAttribute('role', 'button');
      }

      track.appendChild(bar);

      var caption = document.createElement('small');
      caption.textContent = row.label;

      column.appendChild(track);
      column.appendChild(caption);
      host.appendChild(column);

      var percent = Math.round(((Number(row.value) || 0) / max) * 100);
      requestAnimationFrame(function () {
        setTimeout(function () {
          bar.style.height = Math.max(percent, row.value > 0 ? 4 : 2) + '%';
        }, index * 28);
      });
    });
  };

  SB.charts.meter = function (host, value, total, color) {
    var percent = total > 0 ? Math.round((value / total) * 100) : 0;
    host.style.width = percent + '%';
    host.style.background = color || 'var(--green-600)';
  };
})(window);
