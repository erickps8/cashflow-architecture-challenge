import React from 'react';
import ReactDOM from 'react-dom/client';
import AppModern from './AppModern';
import './styles.css';
import './filter-overrides.css';
import './monthly.css';
import './entries.css';
import './forms-modern.css';
import './global-create.css';
import './auth-mobile-fixes.css';
import './mobile-safe-area.css';

ReactDOM.createRoot(document.getElementById('root')!).render(
  <React.StrictMode>
    <AppModern />
  </React.StrictMode>,
);
