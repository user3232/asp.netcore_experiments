/** @jsx jsx */
import { css, jsx } from '@emotion/core';
import { fontFamily, fontSize, gray2 } from './Styles';
import React, { lazy, Suspense } from 'react';
import { BrowserRouter, Route, Redirect, Switch } from 'react-router-dom';
import { Provider } from 'react-redux';
import { configureStore } from './Store';

import { HeaderWithRouter as Header } from './Header';
// import { HomePage } from './HomePage';
import HomePage from './HomePage';

import { SearchPage } from './SearchPage';
import { SignInPage } from './SignInPage';
import { NotFoundPage } from './NotFoundPage';
import { QuestionPage } from './QuestionPage';

// import { AskPage } from './AskPage';
// make component lazy load:
const AskPage = lazy(() => import('./AskPage'));

const store = configureStore();

const App: React.FC = () => {
  return (
    <Provider store={store}>
      <BrowserRouter>
        <div
          css={css`
            font-family: ${fontFamily};
            font-size: ${fontSize};
            color: ${gray2};
          `}
        >
          <Header />

          {/* We would like to select one of pages based on route */}
          <Switch>
            {/* 
          We'd like a '/home' path to render the HomePage component, 
          as well as the '/' path. (Needs <Switch></Switch> and <Redirect ... />)
        */}
            <Redirect from="/home" to="/" />
            {/* 
          We can tell the Route component that renders 
          the HomePage component to do an exact match on
          the location in the browser.
        */}
            <Route exact path="/" component={HomePage} />

            <Route path="/search" component={SearchPage} />
            {/* lazy loading component (needs dummy in the mean time) */}
            <Route path="/ask">
              <Suspense
                fallback={
                  // dummy:
                  <div
                    css={css`
                      margin-top: 100px;
                      text-align: center;
                    `}
                  >
                    Loading...
                  </div>
                }
              >
                {/* this lazy load: */}
                <AskPage />
              </Suspense>
            </Route>
            <Route path="/signin" component={SignInPage} />
            <Route path="/questions/:questionId" component={QuestionPage} />

            {/* 
          no path matches all routes, 
          must be last statement of switch! 
        */}
            <Route component={NotFoundPage} />
          </Switch>
        </div>
      </BrowserRouter>
    </Provider>
  );
};

export default App;
