/** @jsx jsx */
import { css, jsx } from '@emotion/core';
import { QuestionData } from './QuestionsData';
import { gray2, gray3 } from './Styles';
// eslint-disable-next-line @typescript-eslint/no-unused-vars
import React, { FC } from 'react';
import { Link } from 'react-router-dom';

interface Props {
  data: QuestionData;
  showContent?: boolean;
}

export const Question: FC<Props> = ({ data, showContent = true }) => (
  <div
    css={css`
      padding: 10px 0px;
    `}
  >
    {/* question title */}
    <Link
      to={`questions/${data.questionId}`}
      css={css`
        padding: 10px 0px;
        font-size: 19px;
      `}
    >
      {data.title}
    </Link>
    {/* question text */}
    {showContent && (
      <div
        css={css`
          padding-bottom: 10px;
          font-size: 15px;
          color: ${gray2};
        `}
      >
        {/* fragment of question text */}
        {data.content.length > 50
          ? `${data.content.substring(0, 50)}...`
          : data.content}
      </div>
    )}

    {/* question user name and post date */}
    <div
      css={css`
        font-size: 12px;
        font-style: italic;
        color: ${gray3};
      `}
    >
      {`
        Asked by ${data.userName} on
        ${data.created.toLocaleDateString()} 
        ${data.created.toLocaleTimeString()}
      `}
    </div>
  </div>
);
