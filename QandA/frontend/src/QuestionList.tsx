import { FC /* , memo  */ } from 'react';
/** @jsx jsx */
import { css, jsx } from '@emotion/core';
import { gray5, accent2 } from './Styles';
import { QuestionData } from './QuestionsData';
import { Question } from './Question';
// eslint-disable-next-line @typescript-eslint/no-unused-vars
import React from 'react';

interface Props {
  data: QuestionData[];
  renderItem?: (item: QuestionData) => JSX.Element;
}

export const QuestionList: FC<Props> = ({ data, renderItem }) => {
  console.log('Rendering QuestionList', data, renderItem);
  return (
    <ul
      css={css`
        list-style: none;
        margin: 10px 0 0 0;
        padding: 0px 20px;
        background-color: #fff;
        border-bottom-left-radius: 4px;
        border-bottom-right-radius: 4px;
        border-top: 3px solid ${accent2};
        box-shadow: 0 3px 5px 0 rgba(0, 0, 0, 0.16);
      `}
    >
      {data.map((question) => (
        <li
          key={question.questionId}
          css={css`
            border-top: 1px solid ${gray5};
            :first-of-type {
              border-top: none;
            }
          `}
        >
          {renderItem ? renderItem(question) : <Question data={question} />}
        </li>
      ))}
    </ul>
  );
};

/*
  React.momo function only checks for prop changes and if no changes
  React will skip rendering the component, and reuse the last rendered
  result.

  - If your function component wrapped in React.memo has a useState or
    useContext Hook in its implementation, it will still rerender when
    state or context change.
  - By default it will only shallowly compare complex objects in the
    props object.
  - You can also provide a custom comparison function as the second
    argument.

  Use whan your component renders the same result given the same
  props.
*/

// const QuestionListMemo = memo(QuestionList);
