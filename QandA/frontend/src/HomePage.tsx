/** @jsx jsx */
import { css, jsx } from '@emotion/core';
import { PrimaryButton } from './Styles';
import { useEffect, /* useState, */ FC } from 'react';
import { RouteComponentProps } from 'react-router-dom';
import { connect } from 'react-redux';
import { ThunkDispatch } from 'redux-thunk';
import { AnyAction } from 'redux';

import { getUnansweredQuestionsActionCreator, AppState } from './Store';
import { QuestionData } from './QuestionsData';
import { QuestionList } from './QuestionList';
import { Page } from './Page';
import { PageTitle } from './PageTitle';

interface Props extends RouteComponentProps {
  getUnansweredQuestions: () => Promise<void>;
  questions: QuestionData[] | null;
  questionsLoading: boolean;
}

const HomePage: FC<Props> = ({
  history /* history is (default) property of RouteComponentProps object */,
  questions,
  questionsLoading,
  getUnansweredQuestions,
}) => {
  console.log('Home re-rendered');

  // hook state to FunctionComponent
  // returns stateful value and function to update it
  // const [questions, setQuestions] = useState<QuestionData[] | null>(null);
  // const [questionsLoading, setQuestionsLoading] = useState(true);

  // counter changing whole component state when Ask button clicked
  // simulates unneccessary rendering of QuestionList whan not memo(QuestionList)
  // const [count, setCount] = useState(0);

  // side effect when rendering component
  // or one of variable in [...] has changed
  useEffect(
    () => {
      // console.log('Fetching data');
      // const doGetUnansweredQuestions = async () => {
      //   const unansweredQuestions = await getUnansweredQuestions();
      //   setQuestions(unansweredQuestions);
      //   setQuestionsLoading(false);
      // };
      // doGetUnansweredQuestions();
      if (questions === null) {
        getUnansweredQuestions();
      }
    },
    [questions, getUnansweredQuestions],
    // [], // <- without this it is infinite loop!!!!
    // this causes to run useEffect only once!!!
  );

  // component event handler (used on PrimaryButton)
  const handleAskQuestionClick = () => {
    // setCount(count + 1);
    console.log('TODO - move to the AskPage');
    // programically navigate to the AskPage
    history.push('/ask');
  };

  return (
    <Page>
      <div
        css={css`
          display: flex;
          align-items: center;
          justify-content: space-between;
        `}
      >
        <PageTitle>Unanswered questions</PageTitle>
        <PrimaryButton onClick={handleAskQuestionClick}>
          Ask a question
        </PrimaryButton>
      </div>
      {questionsLoading ? (
        <div
          css={css`
            font-size: 16px;
            font-style: italic;
          `}
        >
          Loading...
        </div>
      ) : (
        <QuestionList data={questions || []} />
      )}
    </Page>
  );
};

const mapStateToProps = (store: AppState) => {
  return {
    questions: store.questions.unanswered,
    questionsLoading: store.questions.loading,
  };
};

const mapDispatchToProps = (
  dispatch: ThunkDispatch<
    any, // asynchronous function result type
    any, // asynchronous function parameter type
    AnyAction // the last action created type
  >,
) => {
  return {
    getUnansweredQuestions: () =>
      dispatch(getUnansweredQuestionsActionCreator()),
  };
};

export default connect(mapStateToProps, mapDispatchToProps)(HomePage);
