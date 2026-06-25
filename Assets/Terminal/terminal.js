const sessions = {};
let activeSessionId = null;

// Communicates with C# host
function postMessage(obj) {
    if (window.chrome && window.chrome.webview) {
        window.chrome.webview.postMessage(obj);
    }
}

window.addEventListener('resize', () => {
    if (activeSessionId && sessions[activeSessionId]) {
        sessions[activeSessionId].fitAddon.fit();
    }
});

// Listen to C# messages
if (window.chrome && window.chrome.webview) {
    window.chrome.webview.addEventListener('message', event => {
        const msg = event.data;
        switch (msg.type) {
            case 'createSession':
                createSession(msg.sessionId, msg.title);
                break;
            case 'output':
                writeOutput(msg.sessionId, msg.data);
                break;
            case 'activateSession':
                activateSession(msg.sessionId);
                break;
            case 'closeSession':
                closeSession(msg.sessionId);
                break;
            case 'setTheme':
                setTheme(msg.theme);
                break;
        }
    });
}

function createSession(sessionId, title) {
    if (sessions[sessionId]) return;

    const termElement = document.createElement('div');
    termElement.id = 'term-' + sessionId;
    termElement.style.width = '100%';
    termElement.style.height = '100%';
    termElement.style.display = 'none';
    document.getElementById('terminal-container').appendChild(termElement);

    const term = new Terminal({
        cursorBlink: true,
        fontFamily: 'Cascadia Mono, Courier New, monospace',
        fontSize: 13,
        theme: {
            background: '#1e1e1e',
            foreground: '#d4d4d4',
            cursor: '#aeafad'
        }
    });

    const fitAddon = new FitAddon.FitAddon();
    term.loadAddon(fitAddon);
    term.open(termElement);

    term.onData(data => {
        postMessage({
            type: 'input',
            sessionId: sessionId,
            data: data
        });
    });

    term.onResize(size => {
        postMessage({
            type: 'resize',
            sessionId: sessionId,
            cols: size.cols,
            rows: size.rows
        });
    });

    sessions[sessionId] = {
        term: term,
        fitAddon: fitAddon,
        element: termElement
    };

    // Send initial size
    setTimeout(() => {
        fitAddon.fit();
        postMessage({
            type: 'resize',
            sessionId: sessionId,
            cols: term.cols,
            rows: term.rows
        });
    }, 100);
}

function writeOutput(sessionId, data) {
    const session = sessions[sessionId];
    if (session) {
        session.term.write(data);
    }
}

function activateSession(sessionId) {
    for (const id in sessions) {
        sessions[id].element.style.display = 'none';
    }

    const session = sessions[sessionId];
    if (session) {
        session.element.style.display = 'block';
        activeSessionId = sessionId;
        setTimeout(() => {
            session.fitAddon.fit();
            postMessage({
                type: 'resize',
                sessionId: sessionId,
                cols: session.term.cols,
                rows: session.term.rows
            });
            session.term.focus();
        }, 50);
    }
}

function closeSession(sessionId) {
    const session = sessions[sessionId];
    if (session) {
        session.term.dispose();
        session.element.remove();
        delete sessions[sessionId];
        if (activeSessionId === sessionId) {
            activeSessionId = null;
        }
    }
}

function setTheme(theme) {
    const isDark = theme === 'dark';
    const bg = isDark ? '#1e1e1e' : '#f3f3f3';
    const fg = isDark ? '#d4d4d4' : '#333333';
    const cursor = isDark ? '#aeafad' : '#333333';

    for (const id in sessions) {
        sessions[id].term.options.theme = {
            background: bg,
            foreground: fg,
            cursor: cursor
        };
    }
}

// Signal C# that we are loaded and ready
postMessage({ type: 'ready' });
