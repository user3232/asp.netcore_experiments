/** @jsx jsx */
import { css, jsx } from '@emotion/core';
import { gray3, gray6 } from './Styles';

import { FC, useState, Fragment, useEffect } from 'react';
import { RouteComponentProps } from 'react-router-dom';

import {
  HubConnectionBuilder,
  HubConnectionState,
  HubConnection,
} from '@aspnet/signalr';

import {
  QuestionData,
  getQuestion,
  postAnswer,
  mapQuestionFromServer,
  QuestionDataFromServer,
} from './QuestionsData';
import { Page } from './Page';
import { AnswerList } from './AnswerList';
import { Form, required, minLength, Values } from './Form';
import { Field } from './Field';

interface RouteParams {
  questionId: string;
}

export const QuestionPage: FC<RouteComponentProps<RouteParams>> = ({
  // params injected by routing component
  // -> route (what is displayed by GUI)
  match,
}) => {
  const [question, setQuestion] = useState<QuestionData | null>(null);
  const setUpSignalRConnection = async (questionId: number) => {
    // setup connection to real-time SignalR API
    const connection = new HubConnectionBuilder()
      .withUrl('https://localhost:5001/questionshub')
      // .withUrl("https://localhost:5001/questionshub")
      .withAutomaticReconnect()
      .build();

    // handle Message function being called
    connection.on('Message', (message: string) => {
      console.log('Message', message);
    });

    // handle ReceiveQuestion function being called
    connection.on('ReceiveQuestion', (question: QuestionDataFromServer) => {
      console.log('ReceiveQuestion', question);
      // state update
      // will cause GUI update
      setQuestion(mapQuestionFromServer(question));
    });

    // start the connection
    try {
      await connection.start();
    } catch (err) {
      console.log(err);
    }

    // subscribe to question
    if (connection.state === HubConnectionState.Connected) {
      connection
        // SubscribeQuestion is method on server
        .invoke('SubscribeQuestion', questionId)
        // catch errors
        .catch((err: Error) => {
          return console.error(err.toString());
        });
    }

    // return the connection
    return connection;
  };

  // unsubscribe
  const cleanUpSignalRConnection = async (
    questionId: number,
    connection: HubConnection,
  ) => {
    if (connection.state === HubConnectionState.Connected) {
      // unsubscribe from the question
      try {
        await connection.invoke('UnsubscribeQuestion', questionId);
      } catch (err) {
        console.log(err.toString());
      }
      // stop the connection
      connection.off('Message');
      connection.off('ReceiveQuestion');
      connection.stop();
    } else {
      // stop the connection
      connection.off('Message');
      connection.off('ReceiveQuestion');
      connection.stop();
    }
  };

  useEffect(
    // imperative function to run (on all state changes or listed object changes)
    // which can return cleanup function
    () => {
      // client websocket connection context
      let connection: HubConnection;
      // client http call function definition
      const doGetQuestion = async (questionId: number): Promise<void> => {
        const foundQuestion = await getQuestion(questionId);
        setQuestion(foundQuestion);
      };

      // match is routing context variable
      // match.params.questionId is string!!!
      if (match.params.questionId) {
        // parse string -> number
        const questionId = Number(match.params.questionId);
        // get question using http
        doGetQuestion(questionId);
        // connect to server ws
        // and store connection if successful
        setUpSignalRConnection(questionId).then((conn) => {
          connection = conn;
        });
      }

      // following will be called when this React component unmounts
      return function cleanUp() {
        if (match.params.questionId) {
          const questionId = Number(match.params.questionId);
          cleanUpSignalRConnection(questionId, connection);
        }
      };
    },
    // run above only when rendered or different questionId
    [match.params.questionId],
  );

  const handleSubmit = async (values: Values) => {
    const result = await postAnswer({
      // A non-null assertion operator ( ! )
      // tells the TypeScript compiler that the variable
      // before it cannot be null or undefined.
      questionId: question!.questionId,
      content: values.content,
      userName: 'Fred',
      created: new Date(),
    });
    return { success: result ? true : false };
  };

  return (
    <Page>
      {/* question shadow box */}
      <div
        css={css`
          background-color: white;
          padding: 15px 20px 20px 20px;
          border-radius: 4px;
          border: 1px solid ${gray6};
          box-shadow: 0 3px 5px 0 rgba(0, 0, 0, 0.16);
        `}
      >
        {/* question fonts and margin */}
        <div
          css={css`
            font-size: 19px;
            font-weight: bold;
            margin: 10px 0px 5px;
          `}
        >
          {/* displays existing question */}
          {question !== null && (
            <Fragment>
              <p
                css={css`
                  margin-top: 0px;
                  background-color: white;
                `}
              >
                {question.content}
              </p>
              <div
                css={css`
                  font-size: 12px;
                  font-style: italic;
                  color: ${gray3};
                `}
              >
                {`
                  Asked by ${question.userName} on
                  ${question.created.toLocaleDateString()}
                  ${question.created.toLocaleTimeString()}
                `}
              </div>
              {/* answers for question */}
              <AnswerList data={question.answers} />
              {/* answer submission */}
              <div
                css={css`
                  margin-top: 20px;
                `}
              >
                <Form
                  submitCaption="Submit Your Answer"
                  validationRules={{
                    content: [
                      { validator: required },
                      { validator: minLength, arg: 50 },
                    ],
                  }}
                  onSubmit={handleSubmit}
                  failureMessage="There was a problem with your answer"
                  successMessage="Your answer was successfully submitted"
                >
                  <Field name="content" label="Your Answer" type="TextArea" />
                </Form>
              </div>
            </Fragment>
          )}
        </div>
      </div>
    </Page>
  );
};
