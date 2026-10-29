(function (global) {
  var SB = global.SB = global.SB || {};

  var listeners = [];
  var source = null;
  var pollTimer = null;
  var state = 'off';
  var misses = 0;
  var lastSignalAt = Date.now();

  function emit(signal) {
    lastSignalAt = Date.now();

    listeners.slice().forEach(function (handler) {
      try {
        handler(signal);
      } catch (error) {
        console.warn('Realtime handler', error);
      }
    });
  }

  function setState(next) {
    if (state === next) {
      return;
    }

    state = next;

    listeners.slice().forEach(function (handler) {
      try {
        handler({ kind: 'lidhje', statusKey: next });
      } catch (error) {
        console.warn('Realtime status handler', error);
      }
    });
  }

  function stopPolling() {
    if (pollTimer) {
      clearInterval(pollTimer);
      pollTimer = null;
    }
  }

  function startPolling(intervalMs) {
    if (pollTimer) {
      return;
    }

    setState('polling');

    pollTimer = setInterval(function () {
      if (document.hidden) {
        return;
      }

      emit({ kind: 'rimarrje', statusKey: 'poll' });
    }, intervalMs || 6000);
  }

  function openStream(url, intervalMs) {
    if (typeof global.EventSource !== 'function') {
      startPolling(intervalMs);
      return;
    }

    closeStream();

    try {
      source = new global.EventSource(url, { withCredentials: true });
    } catch (error) {
      startPolling(intervalMs);
      return;
    }

    source.addEventListener('open', function () {
      misses = 0;
      setState('live');
    });

    source.addEventListener('ndryshim', function (event) {
      var signal = null;

      try {
        signal = JSON.parse(event.data);
      } catch (error) {
        signal = null;
      }

      if (signal && signal.kind !== 'zemrat') {
        emit(signal);
      }
    });

    source.addEventListener('error', function () {
      misses += 1;

      if (source && source.readyState === 2) {
        setState('reconnect');
      }

      if (misses >= 4) {
        closeStream();
        startPolling(intervalMs);
        return;
      }

      setState('reconnect');
      global.setTimeout(function () {
        if (state !== 'live') {
          openStream(url, intervalMs);
        }
      }, 3000);
    });
  }

  function closeStream() {
    if (source) {
      source.close();
      source = null;
    }
  }

  SB.realtime = {
    connect: function (options) {
      var settings = options || {};
      openStream(settings.url || 'api/realtime/stream', settings.pollMs || 6000);

      return function () {
        SB.realtime.disconnect();
      };
    },

    disconnect: function () {
      closeStream();
      stopPolling();
      setState('off');
    },

    on: function (handler) {
      listeners.push(handler);

      return function () {
        listeners = listeners.filter(function (item) {
          return item !== handler;
        });
      };
    },

    state: function () {
      return state;
    },

    idleSeconds: function () {
      return Math.round((Date.now() - lastSignalAt) / 1000);
    },

    wake: function () {
      emit({ kind: 'zgjim', statusKey: 'visibility' });
    }
  };

  document.addEventListener('visibilitychange', function () {
    if (document.hidden) {
      return;
    }

    SB.realtime.wake();

    if (!pollTimer && (!source || source.readyState === 2)) {
      SB.realtime.connect();
    }
  });
})(window);
