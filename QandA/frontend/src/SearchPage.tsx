// eslint-disable-next-line @typescript-eslint/no-unused-vars
import React, { FC, useState, useEffect } from 'react';
/** @jsx jsx */
import { css, jsx } from '@emotion/core';

import { Page } from './Page';
import { RouteComponentProps } from 'react-router-dom';
import { QuestionList } from './QuestionList';
import { searchQuestions, QuestionData } from './QuestionsData';

export const SearchPage: FC<RouteComponentProps> = ({ location }) => {
  const [questions, setQuestions] = useState<QuestionData[]>([]);
  const searchParams = new URLSearchParams(location.search);
  const search = searchParams.get('criteria') || '';

  useEffect(() => {
    const doSearch = async (criteria: string) => {
      const foundResults = await searchQuestions(criteria);
      setQuestions(foundResults);
    };
    doSearch(search);
  }, [search]);

  return (
    <Page title="Search Results">
      {search && (
        <p
          css={css`
            font-size: 16px;
            font-style: italic;
            margin-top: 0px;
          `}
        >
          for "{search}"
        </p>
      )}
      <QuestionList data={questions} />
    </Page>
  );
};
